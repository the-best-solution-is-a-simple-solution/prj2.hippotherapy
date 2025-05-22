import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:frontend/pages/registration_page.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  final int extraWaitSeconds = 4;

  String end2EndEmail = "";

  group('Registration Page Integration Tests', () {
    setUpAll(() async {
      // clear data
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http.delete(Uri.parse(url)).timeout(const Duration(seconds: 30));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http.delete(Uri.parse(url)).timeout(const Duration(seconds: 30));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);
    });

    Future<void> navigateToRegisterPage(final WidgetTester tester) async {
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

      // Wait for the app to settle
      await tester.pumpAndSettle();

      // Click the hamburger menu
      final hamburgerMenuButton =
          find.byKey(const Key('hippotherapy_hamburger_menu'));
      await tester.tap(hamburgerMenuButton);
      await tester.pumpAndSettle();

      // await tester.pump(Duration(seconds: 10));

      // Tap on the 'Register' option
      await tester.tap(find.text("Register").first);
      await tester.pumpAndSettle();
    }

    Future<void> fillLoginForm(final WidgetTester tester,
        {final String email = 'test@example.com',
        final String password = 'Password123!'}) async {
      await tester.enterText(find.byType(TextFormField).at(0), email);
      await tester.enterText(find.byType(TextFormField).at(1), password);
      await tester.pumpAndSettle();
    }

    Future<void> pumpRegistrationPage(final WidgetTester tester) async {
      // Ensure window size is reset after the test
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      // Pump the RegistrationPage widget
      await tester.pumpWidget(const MaterialApp(home: RegistrationPage()));
      await tester.pumpAndSettle();
    }

    //verify the email
    Future<String> verifyEmail(final email) async {
      final AllOobCodes = await http
          .get(Uri.parse(
              'http://localhost:9099/emulator/v1/projects/hippotherapy/oobCodes'))
          .timeout(const Duration(seconds: 15));
      final AllOobCodesData = jsonDecode(AllOobCodes.body);
      final allOobCodes = AllOobCodesData['oobCodes'];
      final count = allOobCodes.length;
      final user = allOobCodes[count - 1];
      final link = user['oobLink'];

      final resp = await http.get(Uri.parse(link), headers: <String, String>{
        'User-Agent': 'Dart program',
      });

      return resp.body;
    }

    // Missing required field(s)
    testWidgets('should show error for missing required fields in Registration',
        (final tester) async {
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

      // click hamburger menu
      final hamburgerMenuButton =
          find.byKey(const Key('hippotherapy_hamburger_menu'));
      await tester.tap(hamburgerMenuButton);
      // Wait for the menu to open
      await tester.pumpAndSettle();

      // Reset window
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      await tester.pumpWidget(const MaterialApp(home: RegistrationPage()));
      await tester.pumpAndSettle();

      // Ensure the register button is visible by scrolling
      await tester.ensureVisible(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();

      // Tap the submit button
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();

      // Check for validation error messages
      expect(find.text('Email is required'), findsOneWidget);
      expect(find.text('Password is required'), findsOneWidget);
      expect(find.text('First name is required'), findsOneWidget);
      expect(find.text('Last name is required'), findsOneWidget);
    });

    testWidgets('should show error for invalid email format',
        (final tester) async {
      // Reset window
      addTearDown(() {
        tester.view.resetPhysicalSize();
        tester.view.resetDevicePixelRatio();
      });

      await tester.pumpWidget(const MaterialApp(home: RegistrationPage()));
      await tester.pumpAndSettle();

      // Ensure the register button is visible by scrolling
      await tester.ensureVisible(find.byKey(const ValueKey('emailField')));
      await tester.enterText(
          find.byKey(const ValueKey('emailField')), 'invalid-format');
      await tester.pumpAndSettle();

      // Check for validation error messages
      expect(find.text('Please enter a valid email'), findsOneWidget);
    });

    testWidgets('Email input does not exceed 30 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      const String longEmail =
          'email@email.comaaaaaaaaaaaaaaaa'; // 31 characters
      await tester.enterText(
          find.byKey(const ValueKey('emailField')), longEmail);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Email must be at most 30 characters'), findsNothing);
    });

    testWidgets(
        'Entering a password without an uppercase letter should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter password without an uppercase letter
      await tester.ensureVisible(find.byKey(const ValueKey('passwordField')));
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'password1!');
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(
          find.text('Password must contain at least one uppercase character'),
          findsOneWidget);
    });

    testWidgets('Entering a password without a number should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter password without a number
      await tester.ensureVisible(find.byKey(const ValueKey('passwordField')));
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password!');
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(find.text('Password must contain at least one number'),
          findsOneWidget);
    });

    testWidgets(
        'Entering a password without a lowercase letter should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter password without a lowercase letter
      await tester.ensureVisible(find.byKey(const ValueKey('passwordField')));
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'PASSWORD1!');
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(
          find.text('Password must contain at least one lowercase character'),
          findsOneWidget);
    });

    testWidgets(
        'Entering a password without a special character should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter password without a special character
      await tester.ensureVisible(find.byKey(const ValueKey('passwordField')));
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password1');
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(find.text('Password must contain at least one special character'),
          findsOneWidget);
    });

    testWidgets(
        'Entering a password shorter than 6 characters should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter password shorter than 5 characters
      await tester.ensureVisible(find.byKey(const ValueKey('passwordField')));
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'P@s55');
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(
          find.text('Password must be at least 6 characters'), findsOneWidget);
    });

    testWidgets(
        'Entering a password of 6 characters should not display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter a valid password 5 characters long
      await tester.ensureVisible(find.byKey(const ValueKey('passwordField')));
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'P@s5w1');
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(find.text('Password must be at least 5 characters'), findsNothing);
    });

    testWidgets('Password input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      const String longPassword = 'Password1!aaaaaaaaaa'; // 25 characters
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), longPassword);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Password must be at most 20 characters'), findsNothing);
    });

    // Last name tests
    testWidgets(
        'Entering a last name with 1 character should not display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter last name with 1 character
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'B');
      await tester.pumpAndSettle();

      // Verify that no error message is displayed
      expect(
          find.text('Last name must be at most 20 characters'), findsNothing);
      expect(find.text('Last name is required'), findsNothing);
    });

    testWidgets(
        'Entering a last name with 20 characters should not display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter last name with 20 characters
      final String validLastName = List.filled(20, 'b').join();
      await tester.enterText(
          find.byKey(const ValueKey('lNameField')), validLastName);
      await tester.pumpAndSettle();

      // Verify that no error message is displayed
      expect(
          find.text('Last name must be at most 20 characters'), findsNothing);
      expect(find.text('Last name is required'), findsNothing);
    });

    testWidgets('Last name input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      final String longLastName = 'A' * 25; // 25 characters
      await tester.enterText(
          find.byKey(const ValueKey('lNameField')), longLastName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(
          find.text('Last name must be at most 20 characters'), findsNothing);
    });

    testWidgets('Leaving the last name empty should display a required error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter empty last name
      await tester.enterText(find.byKey(const ValueKey('lNameField')), '');
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));

      // Tap the submit button
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));

      // Verify that the required error message is displayed
      expect(find.text('Last name is required'), findsOneWidget);
      expect(
          find.text('Last name must be at most 20 characters'), findsNothing);
    });

    // First name tests
    // First and last name length limits
    testWidgets(
        'Entering a first name with 1 character should not display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter last name with 1 character
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'B');
      await tester.pumpAndSettle();

      // Verify that no error message is displayed
      expect(
          find.text('First name must be at most 20 characters'), findsNothing);
      expect(find.text('First name is required'), findsNothing);
    });

    testWidgets(
        'Entering a first name with 20 characters should not display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter last name with 20 characters
      final String validLastName = List.filled(20, 'b').join();
      await tester.enterText(
          find.byKey(const ValueKey('fNameField')), validLastName);
      await tester.pumpAndSettle();

      // Verify that no error message is displayed
      expect(
          find.text('Last name must be at most 20 characters'), findsNothing);
      expect(find.text('Last name is required'), findsNothing);
    });

    testWidgets('Last name input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Input a string longer than the max length (20 characters)
      final String longLastName = 'A' * 25;
      await tester.enterText(
          find.byKey(const ValueKey('lNameField')), longLastName);
      await tester.pumpAndSettle();

      // Ensure no error for max length is displayed
      expect(
          find.text('Last name must be at most 20 characters'), findsNothing);
    });

    testWidgets('Leaving the first name empty should display a required error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter empty last name
      await tester.enterText(find.byKey(const ValueKey('fNameField')), '');
      await tester.pumpAndSettle();

      // Tap the submit button
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();

      // Verify that the error message is displayed
      expect(find.text('First name is required'), findsOneWidget);
      expect(
          find.text('First name must be at most 20 characters'), findsNothing);
    });

    // Country tests
    testWidgets(
        'Entering a country with invalid characters should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter country with invalid characters (numbers and symbols)
      await tester.enterText(find.byKey(const ValueKey('countryField')), '1');
      await tester.pumpAndSettle();

      // Verify that  error message is displayed
      expect(find.text('Country must contain only letters and spaces'),
          findsOneWidget);
    });

    testWidgets('Country input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      const String longCountryName = 'aaaaaaaaaaaaaaaaaaaaa';
      await tester.enterText(
          find.byKey(const ValueKey('countryField')), longCountryName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Country must be at most 20 characters'), findsNothing);
    });

    testWidgets('Country input with 1 character is valid',
        (final tester) async {
      await pumpRegistrationPage(tester);

      const String longCountryName = 'a';
      await tester.enterText(
          find.byKey(const ValueKey('countryField')), longCountryName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Country must be at most 20 characters'), findsNothing);
    });

    testWidgets('Country input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      const String longCountryName = 'aaaaaaaaaaaaaaaaaaaaa';
      await tester.enterText(
          find.byKey(const ValueKey('countryField')), longCountryName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Country must be at most 20 characters'), findsNothing);
    });

    testWidgets(
        'Entering a country with valid characters and within 20 characters should not display any error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter a valid country name (letters and spaces, <=20 characters)
      const String validCountry = 'United States';
      await tester.enterText(
          find.byKey(const ValueKey('countryField')), validCountry);
      await tester.pumpAndSettle();

      // Verify that no error messages are displayed
      expect(find.text('Country must contain only letters and spaces'),
          findsNothing);
      expect(find.text('Country must be at most 20 characters'), findsNothing);
    });

    // City tests
    testWidgets(
        'Entering a city with invalid characters should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter city with invalid characters (numbers and symbols)
      await tester.enterText(
          find.byKey(const ValueKey('cityField')), 'City123!');
      await tester.pumpAndSettle();

      // Verify that the specific error message is displayed
      expect(find.text('City must contain only letters and spaces'),
          findsOneWidget);
    });

    testWidgets('City input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      final String longCityName = 'a' * 21;
      await tester.enterText(
          find.byKey(const ValueKey('cityField')), longCityName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('City must be at most 20 characters.'), findsNothing);
    });

    testWidgets('City input of 1 character is valid', (final tester) async {
      await pumpRegistrationPage(tester);

      const String longCityName = 'a';
      await tester.enterText(
          find.byKey(const ValueKey('cityField')), longCityName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('City must be at most 20 characters.'), findsNothing);
    });

    testWidgets('Entering a valid city should not display any error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter a valid city name (letters and spaces, <=20 characters)
      const String validCity = 'New York';
      await tester.enterText(
          find.byKey(const ValueKey('cityField')), validCity);
      await tester.pumpAndSettle();

      // Verify that no error messages are displayed
      expect(
          find.text('City must contain only letters and spaces'), findsNothing);
      expect(find.text('City must be at most 20 characters'), findsNothing);
    });

    // Street tests
    testWidgets(
        'Entering a street with invalid characters should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Ensure the street field is visible by scrolling
      await tester.ensureVisible(find.byKey(const ValueKey('streetField')));

      // Enter street with invalid characters
      await tester.enterText(
          find.byKey(const ValueKey('streetField')), 'Street@123');
      await tester.pumpAndSettle();

      // Trigger validation by tapping the register button
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();

      // Verify that the specific error message is displayed
      expect(
        find.text(
            'Street must contain only letters, numbers, spaces, and common punctuation.'),
        findsOneWidget,
      );
    });

    testWidgets(
        'Entering a street with valid input should not display any error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter a valid street name (letters and spaces, <=20 characters)
      const String validStreet = 'Main Street';
      await tester.enterText(
          find.byKey(const ValueKey('streetField')), validStreet);
      await tester.pumpAndSettle();

      // Verify that no error messages are displayed
      expect(find.text('Street must be at most 20 characters'), findsNothing);
      expect(find.text('Street is required'), findsNothing); // If applicable
    });

    testWidgets('Street input does not exceed 20 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      const String longStreetName = '123 Main Street With More';
      await tester.enterText(
          find.byKey(const ValueKey('streetField')), longStreetName);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Street must be at most 20 characters.'), findsNothing);
    });

    // Postal Code tests

    testWidgets('Entering an invalid postal code should display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Ensure the postal code field is visible
      await tester.ensureVisible(find.byKey(const ValueKey('postalCodeField')));

      // Enter an invalid postal code
      await tester.enterText(
          find.byKey(const ValueKey('postalCodeField')), '12345-6789!');
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();

      // Verify the error message is displayed
      expect(find.text('Postal code should be in the form L#L #L#.'),
          findsOneWidget);
    });

    testWidgets('Entering a valid postal code should not display any error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpRegistrationPage(tester);

      // Enter a valid postal code (letters and numbers, <=20 characters)
      const String validPostalCode = 'A1B3D4';
      await tester.enterText(
          find.byKey(const ValueKey('postalCodeField')), validPostalCode);
      await tester.pumpAndSettle();

      // Verify that no error messages are displayed
      expect(
          find.text('Postal code should be in the form L#L #L#'), findsNothing);
      expect(
          find.text('Postal code must be at most 20 characters'), findsNothing);
      expect(
          find.text('Postal code is required'), findsNothing); // If applicable
    });

    // Phone tests
    testWidgets('Entering a valid phone number should not display any error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      await tester.ensureVisible(find.byKey(const ValueKey('phoneField')));
      // Enter a valid phone number
      await tester.enterText(
          find.byKey(const ValueKey('phoneField')), '123-456-7890');
      await tester.pumpAndSettle();

      // Verify that no error message is displayed
      expect(find.text('Please enter a 10 digit phone number'), findsNothing);
    });

    testWidgets(
        'Entering a phone number with invalid characters should display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter a phone number with invalid characters
      await tester.enterText(
          find.byKey(const ValueKey('phoneField')), '123-456-78AB');
      await tester.pumpAndSettle();

      // Verify that the specific error message is displayed
      expect(find.text('Please enter a 10 digit phone number'), findsOneWidget);
    });

    // Profession tests
    testWidgets(
        'Entering a profession with invalid characters should display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter profession with invalid characters (e.g., numbers and symbols)
      await tester.enterText(
          find.byKey(const ValueKey('professionField')), 'Engineer1!');
      await tester.pumpAndSettle();

      // Verify that the specific error message is displayed
      expect(find.text('Profession must contain only letters and spaces'),
          findsOneWidget);
    });

    testWidgets('Profession input does not exceed 25 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      final String longProfession = 'a' * 26;
      await tester.enterText(
          find.byKey(const ValueKey('professionField')), longProfession);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Profession must be at most 25 characters long'),
          findsNothing);
    });

    testWidgets(
        'Entering a profession with 25 characters should not display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter profession with 16 characters
      const String invalidProfession = 'abcdefghijklmnopqrstuvwxy';
      await tester.enterText(
          find.byKey(const ValueKey('professionField')), invalidProfession);
      await tester.pumpAndSettle();

      // Verify that the specific error message is displayed
      expect(find.text('Profession must be at most 15 characters long'),
          findsNothing);
    });

    testWidgets('Entering a valid profession should not display any error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter a valid profession (letters and spaces, <=15 characters)
      const String validProfession = 'Engineer';
      await tester.enterText(
          find.byKey(const ValueKey('professionField')), validProfession);
      await tester.pumpAndSettle();

      // Verify that no error messages are displayed
      expect(find.text('Profession must contain only letters and spaces'),
          findsNothing);
      expect(find.text('Profession must be at most 15 characters long'),
          findsNothing);
    });

    // Major tests
    testWidgets(
        'Entering a major with invalid characters should display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter major with invalid characters
      await tester.enterText(
          find.byKey(const ValueKey('majorField')), 'Computer1!');
      await tester.pumpAndSettle();

      // Verify that the specific error message is displayed
      expect(find.text('Major must contain only letters and spaces'),
          findsOneWidget);
    });

    testWidgets('Major input does not exceed 25 characters',
        (final tester) async {
      await pumpRegistrationPage(tester);

      final String longMajor = 'a' * 26;
      await tester.ensureVisible(find.byKey(const ValueKey('majorField')));
      await tester.enterText(
          find.byKey(const ValueKey('majorField')), longMajor);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(
          find.text('Major must be at most 25 characters long'), findsNothing);
    });

    testWidgets('Entering a valid major should not display any error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter a valid major
      const String validMajor = 'ComputerScience';
      await tester.enterText(
          find.byKey(const ValueKey('majorField')), validMajor);
      await tester.pumpAndSettle();

      // Verify that no error messages are displayed
      expect(find.text('Major must contain only letters and spaces'),
          findsNothing);
      expect(
          find.text('Major must be at most 15 characters long'), findsNothing);
    });

    // Yearsexp tests
    testWidgets(
        'Entering a valid years of experience should not display any error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      // Enter a valid positive integer
      await tester.enterText(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')), '5');
      await tester.pumpAndSettle();

      // Verify that no error message is displayed
      expect(find.text('Must be an integer'), findsNothing);
      expect(find.text('Must be positive'), findsNothing);
    });

    testWidgets('Entering a non-integer value should display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);

      await tester.ensureVisible(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')));

      // Enter a non integer value
      await tester.enterText(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')), 'a');
      await tester.pumpAndSettle();

      // Verify that the error message is displayed
      expect(find.text('Please enter an integer.'), findsOneWidget);
    });

    testWidgets('Entering a negative integer should display an error',
        (final tester) async {
      await pumpRegistrationPage(tester);
      await tester.ensureVisible(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')));

      // Enter a negative integer
      await tester.enterText(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')), '-1');
      await tester.pumpAndSettle();

      // Verify that the error message is displayed
      expect(find.text('Please enter a positive integer.'), findsOneWidget);
    });

    testWidgets('should register a new therapist successfully',
        (final WidgetTester tester) async {
      await navigateToRegisterPage(tester);
      // Generate a unique email to avoid conflicts
      // IMPORTANT to use the same email as in the next test so it works
      const uniqueEmail = 'userexisting@ex.com';
      final referralResponse =
          await AuthController().generateReferral("12345", uniqueEmail);
      final List<dynamic> responseBody = jsonDecode(referralResponse!.body);

      await tester.enterText(
          find.byKey(const Key("referralField")), responseBody[1]);

      // Enter data into the form fields
      await tester.enterText(
          find.byKey(const ValueKey('emailField')), uniqueEmail);
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password1!');
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'Jane');
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'Smith');
      await tester.enterText(find.byKey(const ValueKey('countryField')), 'USA');
      await tester.enterText(
          find.byKey(const ValueKey('cityField')), 'Los Angeles');
      await tester.enterText(
          find.byKey(const ValueKey('streetField')), '456 Elm St');
      await tester.enterText(
          find.byKey(const ValueKey('postalCodeField')), 'A1B 2C3');
      await tester.enterText(
          find.byKey(const ValueKey('phoneField')), '555-555-1234');
      await tester.enterText(
          find.byKey(const ValueKey('professionField')), 'Therapist');
      await tester.enterText(
          find.byKey(const ValueKey('majorField')), 'Occupational Therapy');
      await tester.enterText(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')), '3');
      await tester.pumpAndSettle();

      // Submit the form
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();
    });

    testWidgets(
        'should display error dialog when registering with an existing email',
        (final WidgetTester tester) async {
      // Navigate to the Register page
      await navigateToRegisterPage(tester);
      await tester.pumpAndSettle();

      // Use an email that is already registered
      const existingEmail = 'userexisting@ex.com';
      final referralResponse =
          await AuthController().generateReferral("12345", existingEmail);
      final List<dynamic> responseBody = jsonDecode(referralResponse!.body);

      await tester.enterText(
          find.byKey(const ValueKey('referralField')), responseBody[1]);

      await tester.enterText(
          find.byKey(const ValueKey('emailField')), existingEmail);
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password123!');
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'John');
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'Doe');

      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 3));
      await tester.pumpAndSettle();

      // Find the SnackBar
      // expect(find.text('Email is already registered.'), findsOneWidget);
      final snackBarFinder = find.byType(SnackBar);
      expect(snackBarFinder, findsOneWidget);

      // Verify the SnackBar contains the expected error message
      expect(
        find.descendant(
          of: snackBarFinder,
          matching: find.text(
              'Registration failed: {"message":"Email userexisting@ex.com is already registered."}'),
        ),
        findsOneWidget,
      );
    });

    testWidgets('end to end test', (final WidgetTester tester) async {
      await navigateToRegisterPage(tester);
      // Generate a unique email to avoid conflicts
      end2EndEmail =
          'user${DateTime.now().millisecondsSinceEpoch % 10000}@ex.com';

      await Future.delayed(const Duration(seconds: 2));

      final referralResponse =
          await AuthController().generateReferral("12345", end2EndEmail);
      final List<dynamic> responseBody = jsonDecode(referralResponse!.body);

      await tester.enterText(
          find.byKey(const Key("referralField")), responseBody[1]);

      // Enter data into the form fields
      await tester.enterText(
          find.byKey(const ValueKey('emailField')), end2EndEmail);
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password1!');
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'John');
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'Smith');
      await tester.enterText(
          find.byKey(const ValueKey('countryField')), 'Canada');
      await tester.enterText(
          find.byKey(const ValueKey('cityField')), 'Saskatoon');
      await tester.enterText(
          find.byKey(const ValueKey('streetField')), '123 Fake St');
      await tester.enterText(
          find.byKey(const ValueKey('postalCodeField')), 'A1B 2C3');
      await tester.enterText(
          find.byKey(const ValueKey('phoneField')), '555-555-1234');
      await tester.enterText(
          find.byKey(const ValueKey('professionField')), 'Therapist');
      await tester.enterText(
          find.byKey(const ValueKey('majorField')), 'Occupational Therapy');
      await tester.enterText(
          find.byKey(const ValueKey('yearsExpInHippotherapyField')), '0');
      await tester.pumpAndSettle();

      // Submit the form
      await tester.tap(find.byKey(const ValueKey('registerButton')));
      await tester.pumpAndSettle();
      // Wait for redirect to homepage

      // expect(find.byType(HomePage), findsOneWidget);
    });

    testWidgets('end to end test', (final WidgetTester tester) async {
      final authController = AuthController();

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
      await fillLoginForm(tester, email: end2EndEmail, password: 'Password1!');

      await tester.tap(find.byKey(const ValueKey('login_button')));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 5));

      await tester.tap(find.text('Profile'));
      await tester.pumpAndSettle();

      // Verify each row's content
      // expect(find.byKey(ValueKey('ID-row')), findsOneWidget);
      // expect(find.text('ID: 12345'), findsOneWidget);

      // Expect values from registration
      expect(find.byKey(const ValueKey('emailRow')), findsOneWidget);
      expect(find.text(end2EndEmail), findsOneWidget);

      expect(find.byKey(const ValueKey('countryRow')), findsOneWidget);
      expect(find.text('Canada'), findsOneWidget);

      expect(find.byKey(const ValueKey('cityRow')), findsOneWidget);
      expect(find.text('Saskatoon'), findsOneWidget);

      expect(find.byKey(const ValueKey('streetRow')), findsOneWidget);
      expect(find.text('123 Fake St'), findsOneWidget);

      expect(find.byKey(const ValueKey('postalCodeRow')), findsOneWidget);
      expect(find.text('A1B 2C3'), findsOneWidget);

      expect(find.byKey(const ValueKey('phoneRow')), findsOneWidget);
      expect(find.text('555-555-1234'), findsOneWidget);

      expect(find.byKey(const ValueKey('professionRow')), findsOneWidget);
      expect(find.text('Therapist'), findsOneWidget);

      expect(find.byKey(const ValueKey('majorRow')), findsOneWidget);
      expect(find.text('Occupational Therapy'), findsOneWidget);

      expect(find.byKey(const ValueKey('experienceRow')), findsOneWidget);
      expect(find.text('0'), findsOneWidget);

      final menuButton = find.byIcon(Icons.menu);
      await tester.tap(menuButton);
      await tester.pumpAndSettle();

      // Look for logout button + logout. Expect home page.
      expect(find.text('Logout'), findsOneWidget);
      await tester.tap(find.text('Logout'));
      await tester.pumpAndSettle();
      expect(find.byType(LoginPage), findsOneWidget);
    });
  });
}
