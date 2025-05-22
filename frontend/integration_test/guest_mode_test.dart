import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_evaluation_controller.dart';
import 'package:frontend/controllers/local_controllers/local_patient_controller.dart';
import 'package:frontend/controllers/local_controllers/local_session_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/pose.dart';
import 'package:frontend/models/session.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:integration_test/integration_test.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  const int extraWaitSeconds = 2;

  const String guestEmail = "guest@test.com";
  const String newGuestEmail = "newguest@test.com";
  const String loginAsGuestButtonText = "Login as Guest";
  const String guestModeInfoText =
      "Provides the core features for evaluating patients All data is saved locally, there is no backup in the cloud. You are responsible for safeguarding your device and data.";

  //region Seed Data

  final Patient patientOne = Patient(
      therapistId: guestEmail,
      id: "local-patient1-id",
      fName: "Local",
      lName: "PatientOne",
      condition: "Stroke",
      phone: "123-123-1111",
      age: 31,
      email: "local1@test.com",
      doctorPhoneNumber: "123-123-1235");

  final Patient patientTwo = Patient(
      therapistId: guestEmail,
      id: "local-patient2-id",
      fName: "Local",
      lName: "PatientTwo",
      condition: "Stroke",
      phone: "123-123-2222",
      age: 32,
      email: "local2@test.com",
      doctorPhoneNumber: "123-123-1235");

  final Session p1Session1 = Session(
      sessionID: "p1-session1-id",
      patientID: patientOne.id!,
      location: "Canada",
      dateTaken: DateTime.now());

  final Session p2Session1 = Session(
      sessionID: "p2-session1-id",
      patientID: patientTwo.id!,
      location: "Canada",
      dateTaken: DateTime(2022, 12, 31));
  final Session p2Session2 = Session(
      sessionID: "p2-session2-id",
      patientID: patientTwo.id!,
      location: "Canada",
      dateTaken: DateTime(2023, 1, 10));

  int poseVal = 0;
  final PatientEvaluation p1Session1PreEval = PatientEvaluation(
      "pre",
      p1Session1.sessionID!,
      "p1-session1-pre-id",
      false,
      "",
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal);

  poseVal = 1;
  final PatientEvaluation p1Session1PostEval = PatientEvaluation(
      "post",
      p1Session1.sessionID!,
      "p1-session1-post-id",
      false,
      "",
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal);

  poseVal = 0;
  final PatientEvaluation p2Session1PreEval = PatientEvaluation(
      "pre",
      p2Session1.sessionID!,
      "p2-session1-pre-id",
      false,
      "",
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal);

  poseVal = 1;
  final PatientEvaluation p2Session1PostEval = PatientEvaluation(
      "post",
      p2Session1.sessionID!,
      "p2-session1-post-id",
      false,
      "",
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal);

  poseVal = -1;
  final PatientEvaluation p2Session2PreEval = PatientEvaluation(
      "pre",
      p2Session2.sessionID!,
      "p2-session2-pre-id",
      false,
      "",
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal);

  poseVal = -2;
  final PatientEvaluation p2Session2PostEval = PatientEvaluation(
      "post",
      p2Session2.sessionID!,
      "p2-session2-post-id",
      false,
      "",
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal,
      poseVal);



  //endregion

  Future<void> pumpHomePage(final WidgetTester tester) async {
    final authController = AuthController();
    // Inject the AuthController into the widget tree
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthController>.value(
        value: authController,
        child: const MaterialApp(
          debugShowCheckedModeBanner: false, // Disable the debug banner
          home: Hippotherapy(),
        ),
      ),
    );

    await tester.pumpAndSettle();
  }

  Future<void> fillLoginForm(final WidgetTester tester,
      {final String email = guestEmail}) async {
    await tester.enterText(find.byKey(const Key("t_email_field")), email);
    await tester.pumpAndSettle();
  }

  Future<void> tapLoginAsGuestButton(final WidgetTester tester) async {
    await tester.tap(find.text(loginAsGuestButtonText));
    await tester.pumpAndSettle();
  }

  Future<void> loginAsGuest(final WidgetTester tester, final String email) async {
    await fillLoginForm(tester, email: email);
    await tapLoginAsGuestButton(tester);
    await tester.pump(const Duration(seconds: 3));

    // Click Ok on popup
    await tester.tap(find.text("Ok"));
    await tester.pumpAndSettle();
    await tester.pump(const Duration(seconds: 2));
  }

  Future<void> logout(final WidgetTester tester) async {
    // Logout
    await tester.tap(find.byIcon(Icons.menu));
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('logout_btn')));
    await tester.pumpAndSettle();
  }

  /// Clear all local data
  Future<void> clearLocalData() async {
    const FlutterSecureStorage storage = FlutterSecureStorage();
    try {
      storage.deleteAll();
      debugPrint('\x1B[32mDeleted all local data\x1B[0m'); // Green text
    }
    catch (e) {
      debugPrint("Could not delete all local data: $e");
    }
  }


  group('Guest Mode Tests', () {
    setUpAll(() async {
      // Doesn't work to seed data in setup, do it instead in method\
      // also seeding doesn't work so just add the data you need in app
      await clearLocalData();
    });


    testWidgets('Therapist tries to login without email', (final tester) async {
      await pumpHomePage(tester);

      // Enter no data for email
      await fillLoginForm(tester, email: '');
      await tapLoginAsGuestButton(tester);

      // Expect error for empty email
      expect(find.text('Email is required'), findsOneWidget);
    });


    testWidgets('Login with invalid email format', (final tester) async {
      await pumpHomePage(tester);

      await fillLoginForm(tester, email: 'invalid-email-format');
      await tapLoginAsGuestButton(tester);

      // Check for validation error message
      expect(find.text('Please enter a valid email'), findsOneWidget);
    });

    testWidgets('Therapist logs in as guest for the first time',
        (final tester) async {
      await pumpHomePage(tester);

      // Enter guest email with no data attached
      await fillLoginForm(tester, email: newGuestEmail);

      // click login as guest
      await tapLoginAsGuestButton(tester);
      await tester.pumpAndSettle();
      await tester.pump(const Duration(seconds: 5));

      // Check message
      expect(find.textContaining(guestModeInfoText), findsOneWidget);
      await tester.pump(const Duration(seconds: 1));

      // Tap Ok
      await tester.tap(find.text("Ok"));
      await tester.pumpAndSettle();

      // should be on patient page
      expect(find.text("Patient List"), findsOneWidget);
      expect(find.text("No patients found."), findsOneWidget);
    });


    testWidgets('Therapist adds a patient', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);

      // Click + to add patient
      // click the create patient widget
      // final patientAdditionWidget = find.byKey(const Key('add_new_patient'));
      final patientAdditionWidget = find.byTooltip('Add a new patient');
      await tester.tap(patientAdditionWidget);
      await tester.pumpAndSettle();

      //region //==========Form Data:==========//
      // First Name
      final firstNameField = find.byKey(const Key('first_name'));
      await tester.enterText(firstNameField, patientOne.fName);

      // Last Name
      final lastNameField = find.byKey(const Key('last_name'));
      await tester.enterText(lastNameField, patientOne.lName);

      // Condition
      final conditionField = find.byKey(const Key('condition'));
      await tester.enterText(conditionField, patientOne.condition);

      // Phone Number
      final phoneField = find.byKey(const Key('phone'));
      await tester.enterText(phoneField, patientOne.phone);

      // Age - <18
      final ageField = find.byKey(const Key('age'));
      await tester.enterText(ageField, patientOne.age.toString());

      // email
      final emailField = find.byKey(const Key('email'));
      await tester.enterText(emailField, patientOne.email);

      // doctor phone
      final doctorPhoneField = find.byKey(const Key('doctor_phone'));
      await tester.enterText(doctorPhoneField, patientOne.doctorPhoneNumber);

      //endregion

      // Submit the form
      final submitButton = find.byKey(const Key('submit_form'));
      await tester.tap(submitButton);
      await tester.pumpAndSettle(); // Wait for any animations and state changes

      // Check the success message
      final successPrompt = find.byKey(const Key('submission_result'));

      // Check for a success message in the popup, only EXACTLY one widget
      expect(successPrompt, findsOneWidget);

      // check that widget's message
      final successMessageFinder = find.descendant(
          of: successPrompt,
          matching: find.textContaining(
              'Patient Local PatientOne was successfully registered with an ID of'));

      // check that the message was found using the Patient's first & last name up above
      expect(successMessageFinder, findsOneWidget);

      // Tap Ok
      await tester.tap(find.text("OK")); // notice capitals for both
      await tester.pumpAndSettle();

      // Tap cancel
      await tester.tap(find.text("Cancel"));
      await tester.pumpAndSettle();
    });


    testWidgets('Therapist logs in to view local patients', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);

      // Should have one patient
      expect(find.text("${patientOne.fName} ${patientOne.lName}"), findsOneWidget);
    });

    testWidgets('Therapist adds a session', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);

      // Click on patient two
      final patientName = find.text("${patientOne.fName} ${patientOne.lName}");
      expect(patientName, findsOneWidget);
      await tester.tap(patientName);
      await tester.pumpAndSettle();

      // Click add session button
      // final patientAdditionWidget = find.byKey(const Key('add_new_Session'));
      final patientAdditionWidget = find.byTooltip('Add a new session for ${patientOne.fName} ${patientOne.lName}.');
      await tester.tap(patientAdditionWidget);
      await tester.pumpAndSettle();

      // Expect popup
      expect(find.text("Are you sure you want to create a new session for ${patientOne.fName} ${patientOne.lName}?"),
          findsOneWidget);
      await tester.tap(find.text("Yes"));
      await tester.pumpAndSettle();
    });


    testWidgets('Therapist adds a evaluation', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);

      // Click on patient two
      final patientName = find.text("${patientOne.fName} ${patientOne.lName}");
      expect(patientName, findsOneWidget);
      await tester.tap(patientName);
      await tester.pumpAndSettle();

      // Click on session
      // Format the date to yyyy-mm-dd
      final String formattedDate = DateFormat('yyyy-MM-dd').format(DateTime.now());
      // Click it
      await tester.tap(find.textContaining(formattedDate));
      await tester.pumpAndSettle();

      // pre-evaluation button
      final preEvalWidgetButton = find.text("Pre Evaluation: Not Started");
      expect(preEvalWidgetButton, findsOneWidget);

      // Click it
      await tester.tap(preEvalWidgetButton);
      await tester.pumpAndSettle();

      // accepts the create option from popup
      await tester.tap(find.text("Create"));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("HeadTab"))); // HEAD TAB
      await tester.pumpAndSettle();

      // HEAD
      await tester.tap(find.byKey(Key(PoseType.HEAD_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // HEAD ANT
      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEG_TWO.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Arm")); // ARM TAB
      await tester.pumpAndSettle();

      // ELBOW EXTENSION
      await tester
          .tap(find.byKey(Key(PoseType.ELBOW_EXTENSION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Thorax")); // THORAX TAB
      await tester.pumpAndSettle();

      // LUMBAR
      await tester.tap(find.byKey(Key(PoseType.LUMBER_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // THORAIC
      await tester.tap(find.byKey(Key(PoseType.THORACIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // check again to see that submit is still not visible
      expect(find.text('submit'), findsNothing);

      await tester.tap(find.text("Hip")); // HIP TAB
      await tester.pumpAndSettle();

      // HIP FLEX
      await tester.tap(find.byKey(Key(PoseType.HIP_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC
      await tester.tap(find.byKey(Key(PoseType.PELVIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC TILT
      await tester
          .tap(find.byKey(Key(PoseType.PELVIC_TILT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // TRUNK NEUTRAL
      await tester.tap(find.byKey(Key(PoseType.TRUNK_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.drag(find.byType(Scrollable),
          const Offset(0, -300)); // got to bottom of the screen
      await tester.pumpAndSettle();

      // TRUNK INCLINATION
      await tester
          .tap(find.byKey(Key(PoseType.TRUNK_INCLINATION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Leg")); // LEG TAB
      await tester.pumpAndSettle();

      // KNEE
      await tester.tap(find.byKey(Key(PoseType.KNEE_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // once everything is done, Submit should be visible, and selectable
      expect(find.text('Submit'), findsOneWidget);

      await tester.tap(find.text("Submit")); // submit button
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 1));

      expect(find.text("Evaluation Submission Received"), findsOneWidget);

      await tester.tap(find.byKey(const Key("submitEvaluationYes")));
      await tester.pumpAndSettle();
    });

    testWidgets('Therapist adds a post evaluation', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);
      await tester.pump(const Duration(seconds: 3));

      // Click on patient two
      final patientName = find.text("${patientOne.fName} ${patientOne.lName}");
      expect(patientName, findsOneWidget);
      await tester.tap(patientName);
      await tester.pumpAndSettle();

      // Click on session
      // Format the date to yyyy-mm-dd
      final String formattedDate = DateFormat('yyyy-MM-dd').format(DateTime.now());
      // Click it
      await tester.tap(find.textContaining(formattedDate));
      await tester.pumpAndSettle();

      // pre-evaluation button
      final postEvalWidgetButton = find.text("Post Evaluation: Not Started");
      expect(postEvalWidgetButton, findsOneWidget);

      // Click it
      await tester.tap(postEvalWidgetButton);
      await tester.pumpAndSettle();

      // accepts the create option from popup
      await tester.tap(find.text("Create"));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("HeadTab"))); // HEAD TAB
      await tester.pumpAndSettle();

      // HEAD
      await tester.tap(find.byKey(Key(PoseType.HEAD_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // HEAD ANT
      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Arm")); // ARM TAB
      await tester.pumpAndSettle();

      // ELBOW EXTENSION
      await tester
          .tap(find.byKey(Key(PoseType.ELBOW_EXTENSION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Thorax")); // THORAX TAB
      await tester.pumpAndSettle();

      // LUMBAR
      await tester.tap(find.byKey(Key(PoseType.LUMBER_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // THORAIC
      await tester.tap(find.byKey(Key(PoseType.THORACIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // check again to see that submit is still not visible
      expect(find.text('submit'), findsNothing);

      await tester.tap(find.text("Hip")); // HIP TAB
      await tester.pumpAndSettle();

      // HIP FLEX
      await tester.tap(find.byKey(Key(PoseType.HIP_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC
      await tester.tap(find.byKey(Key(PoseType.PELVIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC TILT
      await tester
          .tap(find.byKey(Key(PoseType.PELVIC_TILT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // TRUNK NEUTRAL
      await tester.tap(find.byKey(Key(PoseType.TRUNK_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.drag(find.byType(Scrollable),
          const Offset(0, -300)); // got to bottom of the screen
      await tester.pumpAndSettle();

      // TRUNK INCLINATION
      await tester
          .tap(find.byKey(Key(PoseType.TRUNK_INCLINATION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Leg")); // LEG TAB
      await tester.pumpAndSettle();

      // KNEE
      await tester.tap(find.byKey(Key(PoseType.KNEE_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // once everything is done, Submit should be visible, and selectable
      expect(find.text('Submit'), findsOneWidget);

      await tester.tap(find.text("Submit")); // submit button
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 1));

      expect(find.text("Evaluation Submission Received"), findsOneWidget);

      await tester.tap(find.byKey(const Key("submitEvaluationYes")));
      await tester.pumpAndSettle();
    });


    testWidgets('Therapist views an evaluation', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);
      await tester.pump(Duration(seconds: extraWaitSeconds));


      // Click on patient
      final patientName = find.text("${patientOne.fName} ${patientOne.lName}");
      expect(patientName, findsOneWidget);
      await tester.tap(patientName);
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));


      // Click on session
      // Format the date to yyyy-mm-dd
      final String formattedDate = DateFormat('yyyy-MM-dd').format(DateTime.now());
      // Click it
      await tester.tap(find.textContaining(formattedDate));
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));


      // Expect pre and post evaluations completed
      final preEvalBtn = find.text("Pre Evaluation: Completed");
      final postEvalBtn = find.text("Post Evaluation: Completed");
      expect(preEvalBtn, findsOneWidget);
      expect(postEvalBtn, findsOneWidget);

      // Go to pre evaluation
      await tester.tap(preEvalBtn);
      await tester.pumpAndSettle();

      //region  Scroll until each image is visible
      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.HIP_FLEX_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.HIP_FLEX_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.LUMBER_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.LUMBER_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.HEAD_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.HEAD_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.HEAD_ANT_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.HEAD_ANT_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.KNEE_FLEX_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.KNEE_FLEX_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.PELVIC_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.PELVIC_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.PELVIC_TILT_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.PELVIC_TILT_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.THORACIC_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.THORACIC_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.TRUNK_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.TRUNK_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.TRUNK_INCLINATION_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.TRUNK_INCLINATION_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.ELBOW_EXTENSION_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.ELBOW_EXTENSION_NEUTRAL.imgPath)),
          findsOneWidget);
      //endregion
    });

    testWidgets('Therapist views an evaluation', (final tester) async {
      await pumpHomePage(tester);
      await logout(tester);
      await loginAsGuest(tester, guestEmail);
      await tester.pump(Duration(seconds: extraWaitSeconds));

      // Click on patient
      final patientName = find.text("${patientOne.fName} ${patientOne.lName}");
      expect(patientName, findsOneWidget);
      await tester.tap(patientName);
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));


      // Click on session
      // Format the date to yyyy-mm-dd
      final String formattedDate = DateFormat('yyyy-MM-dd').format(
          DateTime.now());
      // Click it
      await tester.tap(find.textContaining(formattedDate));
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));

      // Comparisons tab is visible
      expect(find.text("Comparisons"), findsOneWidget);
      await tester.tap(find.text("Comparisons"));
      await tester.pumpAndSettle();
      await tester.pump(const Duration(seconds: 3));
    });

  });



}
