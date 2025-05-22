import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('Test adding a patient', () {
    setUpAll(() async {
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getSeedOwnerTherapistInfoRoute();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 10));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      // we need to wait for the seeded data to show
      await Future.delayed(const Duration(seconds: 10));
    });

    testWidgets('Simulate the steps to add a patient', (final tester) async {
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
      // Wait for the app to settle
      await tester.pumpAndSettle();

      // login as a therapist
      await tester.enterText(
          find.byKey(const Key('t_email_field')), 'johnsmith1@test.com');
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('t_password_field')), 'Password1!');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('login_button')));
      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 5));

      // click hamburger menu
      await tester.tap(find.byIcon(Icons.menu));
      // Wait for the menu to open
      await tester.pumpAndSettle();

      // click the patient's list tab on the menu
      final patientListItem = find.byKey(const Key('patient_list_main'));
      await tester.tap(patientListItem);
      await tester.pumpAndSettle();

      // click the create patient widget
      final patientAdditionWidget = find.byTooltip('Add a new patient');
      await tester.tap(patientAdditionWidget);
      await tester.pumpAndSettle();

      // start the form:
      // First Name
      final firstNameField = find.byKey(const Key('first_name'));
      await tester.enterText(firstNameField, 'Matthias');

      // Last Name
      final lastNameField = find.byKey(const Key('last_name'));
      await tester.enterText(lastNameField, 'Corvinus');

      // Condition
      final conditionField = find.byKey(const Key('condition'));
      await tester.enterText(conditionField, 'condition');

      // Phone Number
      final phoneField = find.byKey(const Key('phone'));
      await tester.enterText(phoneField, '456-456-9832');

      // Age - <18
      final ageField = find.byKey(const Key('age'));
      await tester.enterText(ageField, '15');

      // email
      final emailField = find.byKey(const Key('email'));
      await tester.enterText(emailField, 'email@domain.com');

      // doctor phone
      final doctorPhoneField = find.byKey(const Key('doctor_phone'));
      await tester.enterText(doctorPhoneField, '123-456-9832');

      // weight
      final weightField = find.byKey(const Key('weight'));
      await tester.enterText(weightField, '101.48');

      // height
      final heightField = find.byKey(const Key('height'));
      await tester.enterText(heightField, '156');

      // guardian phone
      final guardianPhoneField = find.byKey(const Key('guardian_phone'));
      await tester.enterText(guardianPhoneField, '987-456-1234');

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
              'Patient Matthias Corvinus was successfully registered with an ID of'));

      // check that the message was found using the Patient's first & last name up above
      expect(successMessageFinder, findsOneWidget);
    });
  });
}
