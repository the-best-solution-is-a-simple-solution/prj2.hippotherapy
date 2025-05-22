import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/models/pose.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  WidgetsFlutterBinding.ensureInitialized();

  const String johnSmithEmail = "info-therapist1@test.com";
  const String johnSmithPassword = "Password1!";

  Future<void> goToPatientInfo(final WidgetTester tester,
      final String patientName, final bool needLogout) async {
    // Launch app and wait for it to open
    final authController = AuthController();
    // Inject the AuthController into the widget tree
    // Necessary for home page (for now)
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthController>.value(
        value: authController,
        child: const MaterialApp(
          home: Hippotherapy(),
        ),
      ),
    );
    await tester.pumpAndSettle();

    if (needLogout) {
      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('logout_btn')));
      await tester.pumpAndSettle();
    }

    // login as a therapist
    await tester.enterText(
        find.byKey(const Key('t_email_field')), johnSmithEmail);
    await tester.pumpAndSettle();

    await tester.enterText(
        find.byKey(const Key('t_password_field')), johnSmithPassword);
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('login_button')));
    await tester.pumpAndSettle();

    // Open menu
    await tester.tap(find.byIcon(Icons.menu));
    await tester.pumpAndSettle();

    // click patients link
    await tester.tap(find.text('Patients'));
    await tester.pumpAndSettle();

    // click patient name
    await tester.tap(find.text(patientName));
    await tester.pumpAndSettle();
  }

  Future<void> goToPatientListPage(final tester) async {
    // click hamburger menu
    await tester.tap(find.byIcon(Icons.menu));
    // Wait for the menu to open
    await tester.pumpAndSettle();

    final patientListItem = find.byKey(const Key('patient_list_main'));
    await tester.tap(patientListItem);
    await tester.pumpAndSettle();
  }

  Future<void> fillInEvaluation(
      final PoseType headLat,
      final PoseType headAnt,
      final PoseType elbow,
      final PoseType lumbar,
      final PoseType thoracic,
      final PoseType hip,
      final PoseType pelvic,
      final PoseType pelvicTilt,
      final PoseType trunk,
      final PoseType trunkInc,
      final PoseType knee,
      final tester) async {
    await tester.tap(find.byKey(const Key("HeadTab"))); // HEAD TAB
    await tester.pumpAndSettle();

    // HEAD
    await tester.tap(find.byKey(Key(headLat.toString())));
    await tester.pumpAndSettle();

    // HEAD ANT
    await tester.tap(find.byKey(Key(headAnt.toString())));
    await tester.pumpAndSettle();

    await tester.tap(find.text("Arm")); // ARM TAB
    await tester.pumpAndSettle();

    // ELBOW EXTENSION
    await tester.tap(find.byKey(Key(elbow.toString())));
    await tester.pumpAndSettle();

    await tester.tap(find.text("Thorax")); // THORAX TAB
    await tester.pumpAndSettle();

    // LUMBAR
    await tester.tap(find.byKey(Key(lumbar.toString())));
    await tester.pumpAndSettle();

    // THORAIC
    await tester.tap(find.byKey(Key(thoracic.toString())));
    await tester.pumpAndSettle();

    await tester.tap(find.text("Hip")); // HIP TAB
    await tester.pumpAndSettle();

    // HIP FLEX
    await tester.tap(find.byKey(Key(hip.toString())));
    await tester.pumpAndSettle();

    // PELVIC
    await tester.tap(find.byKey(Key(pelvic.toString())));
    await tester.pumpAndSettle();

    // PELVIC TILT
    await tester.tap(find.byKey(Key(pelvicTilt.toString())));
    await tester.pumpAndSettle();

    // TRUNK NEUTRAL
    await tester.tap(find.byKey(Key(trunk.toString())));
    await tester.pumpAndSettle();

    await tester.drag(find.byType(Scrollable),
        const Offset(0, -300)); // got to bottom of the screen
    await tester.pumpAndSettle();

    // TRUNK INCLINATION
    await tester.tap(find.byKey(Key(trunkInc.toString())));
    await tester.pumpAndSettle();

    await tester.tap(find.text("Leg")); // LEG TAB
    await tester.pumpAndSettle();

    // KNEE
    await tester.tap(find.byKey(Key(knee.toString())));
    await tester.pumpAndSettle();
  }

  group('Test using tagging system in notes', () {
    setUpAll(() async {
// seed data
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getSeedPatientInfoGraphTabRoute();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 15));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      await Future.delayed(const Duration(seconds: 5));
    });
    testWidgets(
        'Test that adding a tag in notes will display a message in form page',
        (final tester) async {
      debugPrint(
          '================================================================================================1');
      await goToPatientInfo(tester, 'Bethany Larson', false);
      await tester.pumpAndSettle();

      // await tester.tap(find.byKey(const Key('add_new_Session')));
      await tester.tap(find.byTooltip(
          "Add a new session for Bethany Larson.")); // create a session
      await tester.pumpAndSettle();

      await tester.pumpAndSettle();
      await tester.tap(find.text('Yes'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Pre Evaluation: Not Started'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Create'));
      await tester.pumpAndSettle();

      await fillInEvaluation(
          PoseType.HEAD_NEUTRAL,
          PoseType.HEAD_ANT_NEUTRAL,
          PoseType.ELBOW_EXTENSION_NEUTRAL,
          PoseType.LUMBER_NEUTRAL,
          PoseType.THORACIC_NEUTRAL,
          PoseType.HIP_FLEX_NEUTRAL,
          PoseType.PELVIC_NEUTRAL,
          PoseType.PELVIC_TILT_NEUTRAL,
          PoseType.TRUNK_NEUTRAL,
          PoseType.TRUNK_INCLINATION_NEUTRAL,
          PoseType.KNEE_FLEX_NEUTRAL,
          tester);
      await tester.pumpAndSettle();

// Navigate to the Notes tab
      await tester.tap(find.text("Notes"));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byType(FormBuilderTextField), "Patient was #unwell");
      await tester.pumpAndSettle();

      expect(
          find.text(
              "Tags preventing this evaluation from being viewed on graph tab: [unwell]"),
          findsOneWidget);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
    });
    testWidgets(
        'Test that adding a note without a tag will not display message in form page',
        (final tester) async {
      debugPrint(
          '================================================================================================2');
      // Go to the patient
      await goToPatientInfo(tester, 'Bethany Larson', true);
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip("Add a new session for Bethany Larson."));

      await tester.pumpAndSettle();
      await tester.tap(find.text('Yes'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Pre Evaluation: Not Started'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Create'));
      await tester.pumpAndSettle();

      await fillInEvaluation(
          PoseType.HEAD_NEUTRAL,
          PoseType.HEAD_ANT_NEUTRAL,
          PoseType.ELBOW_EXTENSION_NEUTRAL,
          PoseType.LUMBER_NEUTRAL,
          PoseType.THORACIC_NEUTRAL,
          PoseType.HIP_FLEX_NEUTRAL,
          PoseType.PELVIC_NEUTRAL,
          PoseType.PELVIC_TILT_NEUTRAL,
          PoseType.TRUNK_NEUTRAL,
          PoseType.TRUNK_INCLINATION_NEUTRAL,
          PoseType.KNEE_FLEX_NEUTRAL,
          tester);
      await tester.pumpAndSettle();

      expect(
          find.text(
              "Tags preventing this evaluation from being viewed on graph tab: "),
          findsNothing);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
    });
    testWidgets(
        'Test that adding note with tag will display message on graph tab',
        (final tester) async {
      debugPrint(
          '================================================================================================3');
      await goToPatientInfo(tester, 'Bethany Larson', true);
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip("Add a new session for Bethany Larson."));

      await tester.pumpAndSettle();
      await tester.tap(find.text('Yes'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Pre Evaluation: Not Started'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Create'));
      await tester.pumpAndSettle();

      await fillInEvaluation(
          PoseType.HEAD_NEUTRAL,
          PoseType.HEAD_ANT_NEUTRAL,
          PoseType.ELBOW_EXTENSION_NEUTRAL,
          PoseType.LUMBER_NEUTRAL,
          PoseType.THORACIC_NEUTRAL,
          PoseType.HIP_FLEX_NEUTRAL,
          PoseType.PELVIC_NEUTRAL,
          PoseType.PELVIC_TILT_NEUTRAL,
          PoseType.TRUNK_NEUTRAL,
          PoseType.TRUNK_INCLINATION_NEUTRAL,
          PoseType.KNEE_FLEX_NEUTRAL,
          tester);
      await tester.pumpAndSettle();

// Navigate to the Notes tab
      await tester.tap(find.text("Notes"));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byType(FormBuilderTextField), "Patient was #sick");
      await tester.pumpAndSettle();

      await tester.tap(find.text("Submit"));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Yes'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('No'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Graph'));
      await tester.pumpAndSettle();

      await Future.delayed(Duration(seconds: 5));

      expect(find.text("Found 0 evaluations, 1 excluded."), findsOneWidget);

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();
    });
    testWidgets(
        'Test that adding note without tag will not display message on graph tab',
        (final tester) async {
      debugPrint(
          '================================================================================================4');
      await goToPatientInfo(tester, 'Aston Hahn', true);
      await tester.pumpAndSettle();

      await tester.tap(find.byTooltip("Add a new session for Aston Hahn."));

      await tester.pumpAndSettle();
      await tester.tap(find.text('Yes'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Pre Evaluation: Not Started'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Create'));
      await tester.pumpAndSettle();

      await fillInEvaluation(
          PoseType.HEAD_NEUTRAL,
          PoseType.HEAD_ANT_NEUTRAL,
          PoseType.ELBOW_EXTENSION_NEUTRAL,
          PoseType.LUMBER_NEUTRAL,
          PoseType.THORACIC_NEUTRAL,
          PoseType.HIP_FLEX_NEUTRAL,
          PoseType.PELVIC_NEUTRAL,
          PoseType.PELVIC_TILT_NEUTRAL,
          PoseType.TRUNK_NEUTRAL,
          PoseType.TRUNK_INCLINATION_NEUTRAL,
          PoseType.KNEE_FLEX_NEUTRAL,
          tester);
      await tester.pumpAndSettle();

// Navigate to the Notes tab
      await tester.tap(find.text("Notes"));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byType(FormBuilderTextField), "Patient was sick");
      await tester.pumpAndSettle();

      await tester.tap(find.text("Submit"));
      await tester.pumpAndSettle();

      await tester.tap(find.text('No'));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Graph'));
      await tester.pumpAndSettle();

      await Future.delayed(Duration(seconds: 5));

      expect(find.text("Found 0 evaluations, 1 excluded."), findsNothing);
      expect(find.text("Found 0 evaluations"), findsOneWidget);
    });
  });
}
