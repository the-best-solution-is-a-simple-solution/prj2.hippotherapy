import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/export_page.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:frontend/widgets/export_page_widgets/patient_name_field.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';
import 'package:tutorial_coach_mark/tutorial_coach_mark.dart';

const String URL_START = '${ServerConfig.address}:${ServerConfig.port}';
const String testsController = ServerConfig.integrationTestsRoute;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('test that tutorials are displayed', () {
    late AuthController authController;

    setUpAll(() async {
      authController = AuthController();
      authController.setTherapistId('test_therapist_id');

      // Clear and seed the database
      String url = '$URL_START/$testsController/clear';
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = '$URL_START/$testsController/seed-archive-data';
      response = await http.post(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      // Wait for seeded data
      await Future.delayed(const Duration(seconds: 5));
    });

    // Method to login and navigate to tutorials page
    Future<void> goToTutorials(final WidgetTester tester) async {
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthController>.value(value: authController),
          ],
          child: const Hippotherapy(),
        ),
      );
      await tester.pumpAndSettle();

      // Login
      await tester.enterText(find.byType(TextField).first, 'archive@test.com');
      await tester.enterText(find.byType(TextField).at(1), 'Password1!');
      await tester.tap(find.text('Login'));
      await tester.pumpAndSettle(const Duration(seconds: 2));

      // Open drawer and go to TutorialPage
      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();
      await tester.tap(find.text("Tutorials"));
      await tester.pumpAndSettle();
    }

    // Method to strictly navigate to tutorials page, no login
    Future<void> goToTutorials2(final WidgetTester tester) async {
      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthController>.value(value: authController),
          ],
          child: const Hippotherapy(),
        ),
      );
      await tester.pumpAndSettle();

      // Open drawer and go to TutorialPage
      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();
      await tester.tap(find.text("Tutorials"));
      await tester.pumpAndSettle();
    }

    // Method to wait for tutorial text to appear
    Future<void> waitForWidget(
        final WidgetTester tester,
        final Finder finder, {
          final Duration timeout = const Duration(seconds: 10),
        }) async {
      final endTime = DateTime.now().add(timeout);
      while (DateTime.now().isBefore(endTime)) {
        await tester.pump(const Duration(milliseconds: 100));
        if (finder.evaluate().isNotEmpty) {
          return;
        }
      }
      throw Exception('Timed out waiting for $finder');
    }

    testWidgets('Test Tutorial list displays expected items', (final tester) async {
        await goToTutorials(tester);

        await tester.pumpAndSettle();

        expect(find.text("Creating, Updating, and Deleting Patients"), findsOneWidget);
        expect(find.text("Creating Patient Sessions"), findsOneWidget);
        expect(find.text("Patient Archive"), findsOneWidget);
        expect(find.text("Reassigning Patients"), findsOneWidget);
    });

    testWidgets('Test Patient list tutorial', (final tester) async {
      await goToTutorials2(tester);
      await tester.pumpAndSettle();

        // Start the Patient List Tutorial
        await tester.tap(find.text("Creating, Updating, and Deleting Patients"));

        // Wait for create patient text
      await waitForWidget(tester, find.text("The Patient List displays all patients assigned to the currently "
          "signed in therapist and allows them to create, update, "
          "and delete patients.\n\n To create a new patient, click the "
          "green + button."));

      // Expect create patient text
      expect(find.text("The Patient List displays all patients assigned to the currently "
          "signed in therapist and allows them to create, update, "
          "and delete patients.\n\n To create a new patient, click the "
          "green + button."), findsOneWidget);

      // Advance tutorial
      // await tester.tapAt(const Offset(100, 100));
      await tester.tap(find.byIcon(Icons.add), warnIfMissed: false);

      // Wait for tutorial text
      await waitForWidget(tester, find.text("To update a patient’s information, click the update patient "
          "button next to the patient’s name that you want to update."));

      // Expect update text
      expect(find.text("To update a patient’s information, click the update patient "
          "button next to the patient’s name that you want to update."), findsOneWidget);

      await tester.tapAt(const Offset(100, 100));


      // Wait for archival text
      await waitForWidget(tester, find.text("Archiving a patient will add the patient to the patient "
          "archive which will prevent therapists from adding or "
          "viewing a patient’s sessions.\n\nClick the archive button"
          " to archive a patient."));

      // Expect archival text
      expect(find.text("Archiving a patient will add the patient to the patient "
          "archive which will prevent therapists from adding or "
          "viewing a patient’s sessions.\n\nClick the archive button"
          " to archive a patient."), findsOneWidget);

      // Advance tutorial
      await tester.tapAt(const Offset(100, 100));
      await tester.pumpAndSettle();

      // Verify we are back at tutorials page
      expect(find.byType(TutorialPage),findsOneWidget);
    });


    testWidgets('Test Archive Tutorial', (final tester) async {
      // Navigate to tutorials
      await goToTutorials2(tester);
      await tester.pumpAndSettle();

      // Start the Archive Tutorial
      await tester.tap(find.text("Patient Archive"));

      // Wait for tutorial text to appear
      await waitForWidget(tester, find.text("The patient archive shows archived patients assigned to the currently signed in therapist. From this list you may either restore or permanently delete a patient. "
          "\n\nTo restore a patient, press the restore button. This will restore a patient to the patient list and allow therapists to view or create sessions for them."));

      // Expect tutorial text
      expect(find.text("The patient archive shows archived patients assigned to the currently signed in therapist. From this list you may either restore or permanently delete a patient. "
          "\n\nTo restore a patient, press the restore button. This will restore a patient to the patient list and allow therapists to view or create sessions for them."),
          findsOneWidget);

      // Advance tutorial
      await tester.tapAt(const Offset(100, 100));

      // Wait for tutorial text
      await waitForWidget(tester, find.text("To permanently delete a patient, "
          "press the delete button. This will permanently delete the "
          "patient and cannot be undone."));

      // Expect delete text
      expect(find.text("To permanently delete a patient, "
          "press the delete button. This will permanently delete the "
          "patient and cannot be undone."), findsOneWidget);

      // Advance tutorial
      await tester.tapAt(const Offset(100, 100));

      await tester.pumpAndSettle();
      // Verify we are back at tutorials page
      expect(find.byType(TutorialPage),findsOneWidget);
    });


    testWidgets('Test Create Patient Session tutorial', (final tester) async {
      await goToTutorials2(tester);

      // Start the Create Patient Session tutorial
      await waitForWidget(tester, find.text("Creating Patient Sessions"));
      await tester.tap(find.text("Creating Patient Sessions"));

      // Wait for tutorial text to appear
      await waitForWidget(tester, find.text("To create a new patient session, from the patient list, "
          "click on the patient’s name you want to create a new "
          "session or evaluation for. You will be brought to the "
          "Patient Info Page.\n\nFrom here, press the green + button "
          "to begin a new patient session."));

      // Expect tutorial text
      expect(find.text("To create a new patient session, from the patient list, "
          "click on the patient’s name you want to create a new "
          "session or evaluation for. You will be brought to the "
          "Patient Info Page.\n\nFrom here, press the green + button "
          "to begin a new patient session."), findsOneWidget);

      // Advance tutorial
      await tester.tap(find.byIcon(Icons.add), warnIfMissed: false);
      // await tester.tapAt(const Offset(100, 100));

      // Wait for tutorial text
      await waitForWidget(tester, find.text("With the session tab selected, you can view a patient's past "
          "sessions, sorted by date.\n\n"
          "Clicking on a session will allow you to see the results of "
          "that particular session."));

      // Expect tutorial text
      expect(find.text("With the session tab selected, you can view a patient's past "
          "sessions, sorted by date.\n\n"
          "Clicking on a session will allow you to see the results of "
          "that particular session."), findsOneWidget);

      // Advance tutorial
      await tester.tapAt(const Offset(100, 100));

      // Wait for tutorial text
      await waitForWidget(tester, find.text("From the graph tab, you can view a patient’s session data over "
          "time. From here you can select a date range of sessions "
          "to view. You can also decide whether to view data by "
          "averages or not."));

      // Expect tutorial text
      expect(find.text("From the graph tab, you can view a patient’s session data over "
          "time. From here you can select a date range of sessions "
          "to view. You can also decide whether to view data by "
          "averages or not."), findsOneWidget);

      // Advance tutorial
      await tester.tapAt(const Offset(100, 100));
      await tester.pumpAndSettle();

      // Verify we are back at TutorialPage
      expect(find.byType(TutorialPage), findsOneWidget);
    });

    testWidgets('Test Patient Reassignment tutorial', (final tester) async {
      await goToTutorials2(tester);

      // Start the patient reassignment tutorial
      await tester.tap(find.text("Reassigning Patients"));

      // Wait for tutorial text to appear
      await waitForWidget(tester, find.text("To assign patients to a different therapist you must first be"
          " logged in as an owner. Once logged in as an owner, navigate"
          " to the Therapists page and select a therapist with patients"
          " under their care.\n\nOnce here, click the Reassign Patients"
          " button to toggle patient reassignment mode."));

      // Expect tutorial text
      expect(find.text("To assign patients to a different therapist you must first be"
          " logged in as an owner. Once logged in as an owner, navigate"
          " to the Therapists page and select a therapist with patients"
          " under their care.\n\nOnce here, click the Reassign Patients"
          " button to toggle patient reassignment mode."), findsOneWidget);

      // Advance tutorial
      await tester.tap(find.byTooltip('Toggle patient reassignment mode'),
          warnIfMissed: false);
      // Wait for tutorial text
      await waitForWidget(tester, find.text("Next, select the checkbox next to the patient(s) you wish "
          "to reassign."));

      // Expect tutorial text
      expect(find.text("Next, select the checkbox next to the patient(s) you wish "
          "to reassign."), findsOneWidget);

      // Advance tutorial
      await tester.tapAt(const Offset(100, 100));

      await waitForWidget(tester, find.text("Finally, tap the 'Reassign to...' button and select the therapist"
          " you would like the patient to be assigned to. If successful,"
          " the patient will now be assigned to the selected therapist."));

      // Expect tutorial text
      expect(find.text("Finally, tap the 'Reassign to...' button and select the therapist"
          " you would like the patient to be assigned to. If successful,"
          " the patient will now be assigned to the selected therapist."), findsOneWidget);

      await tester.tap(find.byTooltip('Choose a new therapist to reassign patients'),
          warnIfMissed: false);

      // Verify we are back at TutorialPage
      await tester.pumpAndSettle();
      expect(find.byType(TutorialPage), findsOneWidget);
    });

    testWidgets('Test Archive Tutorial Skip Functionality', (final tester) async {
      // Initialize auth controller
      await goToTutorials2(tester);

      // Start the Archive Tutorial
      await tester.tap(find.text("Patient Archive"));

      // Wait for the first tutorial step to appear
      await waitForWidget(
        tester,
        find.text(
          "The patient archive shows archived patients assigned to the currently signed in therapist. From this list you may either restore or permanently delete a patient. "
              "\n\nTo restore a patient, press the restore button. This will restore a patient to the patient list and allow therapists to view or create sessions for them.",
        ),
      );

      // Verify the first tutorial text is displayed
      expect(
        find.text(
          "The patient archive shows archived patients assigned to the currently signed in therapist. From this list you may either restore or permanently delete a patient. "
              "\n\nTo restore a patient, press the restore button. This will restore a patient to the patient list and allow therapists to view or create sessions for them.",
        ),
        findsOneWidget,
      );

      // Find and tap the skip button
      final skipButtonFinder = find.text("SKIP");
      expect(skipButtonFinder, findsOneWidget);
      await tester.tap(skipButtonFinder, warnIfMissed: false);
      await tester.pumpAndSettle();

      // Verify the tutorial is no longer visible
      expect(
        find.text(
          "The patient archive shows archived patients assigned to the currently signed in therapist. From this list you may either restore or permanently delete a patient. "
              "\n\nTo restore a patient, press the restore button. This will restore a patient to the patient list and allow therapists to view or create sessions for them.",
        ),
        findsNothing,
      );

      // Verify we're back at tutorial page
      expect(find.byType(TutorialPage), findsOneWidget);
    });
  });
}