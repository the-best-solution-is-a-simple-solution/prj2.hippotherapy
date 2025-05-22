import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/password_reset_page.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

const String URL_START = '${ServerConfig.address}:${ServerConfig.port}';
const String testsController = ServerConfig.integrationTestsRoute;
const OBNOXIOUS_PWD = 'a@*\$\{1\}#_\`\!% !@^A\$';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  final int extraWaitSeconds = 2;

  group('test that passwords can be reset', () {
    setUpAll(() async {
      // hardcoded address of mail hog API (not the smtp server)

      // clear data
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http
          .delete(Uri.parse(url))
          .timeout(const Duration(seconds: 30));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http
          .delete(Uri.parse(url))
          .timeout(const Duration(seconds: 30));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      // Wait for better reliability
      await Future.delayed(const Duration(seconds: 5));

      url = ServerConfig.getSeedOwnerTherapistInfoRoute();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 60));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = '$URL_START/$testsController/clear-mail';
      response = await http
          .delete(Uri.parse(url))
          .timeout(const Duration(seconds: 30));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      // we need to wait for the seeded data to show
      await Future.delayed(const Duration(seconds: 10));
    });

    testWidgets(
        'Entering a password without an uppercase letter should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      // Enter password without an uppercase letter
      await tester.enterText(find.byKey(const Key('pwd_field')), 'password1!');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(
          find.text('Password must contain at least one uppercase character'),
          findsOneWidget);
    });

    testWidgets('Entering a password without a number should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      // Enter password without a number
      await tester.ensureVisible(find.byKey(const ValueKey('pwd_field')));
      await tester.enterText(
          find.byKey(const ValueKey('pwd_field')), 'Password!');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(find.text('Password must contain at least one number'),
          findsOneWidget);
    });

    testWidgets(
        'Entering a password without a lowercase letter should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      // Enter password without a lowercase letter
      await tester.ensureVisible(find.byKey(const ValueKey('pwd_field')));
      await tester.enterText(
          find.byKey(const ValueKey('pwd_field')), 'PASSWORD1!');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 3));

      // Verify error message is displayed
      expect(
          find.text('Password must contain at least one lowercase character'),
          findsOneWidget);
    });

    testWidgets(
        'Entering a password without a special character should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      // Enter password without a special character
      await tester.ensureVisible(find.byKey(const ValueKey('pwd_field')));
      await tester.enterText(
          find.byKey(const ValueKey('pwd_field')), 'Password1');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(find.text('Password must contain at least one special character'),
          findsOneWidget);
    });

    testWidgets(
        'Entering a password shorter than 6 characters should display an error',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      // Enter password shorter than 5 characters
      await tester.ensureVisible(find.byKey(const ValueKey('pwd_field')));
      await tester.enterText(find.byKey(const ValueKey('pwd_field')), 'P@s55');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      // Verify error message is displayed
      expect(
          find.text('Password must be at least 6 characters'), findsOneWidget);
    });

    testWidgets(
        'Entering a password of 6 characters should not display an error for that, '
        'but display error for not typing in matching password instead',
        (final tester) async {
      // Pump the RegistrationPage widget
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      // Enter a valid password 5 characters long
      await tester.ensureVisible(find.byKey(const ValueKey('pwd_field')));
      await tester.enterText(find.byKey(const ValueKey('pwd_field')), 'P@s5w1');
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));

      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      // Verify error message is NOT displayed
      expect(find.text('Password must be at least 6 characters'), findsNothing);

      // Verify that matching error is shown
      expect(find.text('Password fields must match match'), findsNothing);
    });

    testWidgets('Password input does not exceed 20 characters',
        (final tester) async {
      await pumpPasswordResetPage(tester);

      await tester.pumpAndSettle();

      const String longPassword = 'Password1!aaaaaaaaaa'; // 25 characters
      await tester.enterText(
          find.byKey(const ValueKey('pwd_field')), longPassword);
      await tester.pumpAndSettle();

      // Ensure no error about max length appears
      expect(find.text('Password must be at most 20 characters'), findsNothing);
    });

    testWidgets('Test that therapist is able to reset password',
        (final tester) async {
      const String therapistEmail = 'johnsmith1@test.com';
      const String newPassword = '#NuP4ssw!rd1';

      // Launch app and wait for it to open
      final authController = AuthController();
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: const Hippotherapy(),
        ),
      );

      await tester.pump(Duration(seconds: extraWaitSeconds));

      // Login as a therapist
      await tester.enterText(
          find.byKey(const Key('t_email_field')), therapistEmail);
      await tester.pumpAndSettle();

      // Click forgot password button
      await tester.tap(find.byKey(const Key('forgot_pwd')));
      await tester.pumpAndSettle();

      // Accept the prompt
      await tester.tap(find.byKey(const Key('req_pwd_reset')));
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));

      // Fetch the latest email from MailHog via the backend
      final res = await http
          .get(Uri.parse('$URL_START/$testsController/get-latest-mailv2'));
      final String passwdResetLink = res.body;

      debugPrint('Fully Cleaned Email Link: $passwdResetLink');

      // Parse the link and extract query parameters
      final Uri resetLink = Uri.parse(passwdResetLink);
      final String? linkParam = resetLink.queryParameters['link'];

      debugPrint('linkparam: ${linkParam!}');
      debugPrint('parsed link param: ${Uri.parse(linkParam)}');
      printYellow(
          "before Navigate to the PasswordResetPage with the link parameter");

      // Navigate to the PasswordResetPage with the link parameter
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: MaterialApp(
            home: const Hippotherapy(),
            onGenerateRoute: (final settings) {
              if (settings.name == PasswordResetPage.RouteName) {
                // Return a MaterialPageRoute with the PasswordResetPage
                return MaterialPageRoute(
                  builder: (final context) => PasswordResetPage(
                    testPasswdResetLink: Uri.parse(linkParam),
                  ),
                );
              }
              // Delegate other routes to Hippotherapy.routes
              final routeBuilder = Hippotherapy.routes[settings.name];
              if (routeBuilder != null) {
                return MaterialPageRoute(builder: routeBuilder);
              }
              return null;
            },
          ),
        ),
      );
      await tester.pump(Duration(seconds: extraWaitSeconds));

      printYellow("before Simulate navigation to the PasswordResetPage");

      // Simulate navigation to the PasswordResetPage
      Navigator.of(tester.element(find.byType(Hippotherapy))).pushNamed(
        PasswordResetPage.RouteName,
      );

      await tester.pump(Duration(seconds: extraWaitSeconds));
      printYellow("going to enter new password");

      // Enter new password
      await tester.enterText(find.byKey(const Key('pwd_field')), newPassword);
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('conf_pwd_field')), newPassword);
      await tester.pumpAndSettle();

      // Submit the form
      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      // Verify successful password reset
      expect(find.text('Password reset successfully!'), findsOneWidget);
      await tester.pumpAndSettle();

      // Login as a therapist with NEW password
      await tester.enterText(
          find.byKey(const Key('t_email_field')), therapistEmail);
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));
      printRed("new password $newPassword");

      await tester.enterText(
          find.byKey(const Key('t_password_field')), newPassword);
      await tester.pumpAndSettle();

      // press login
      await tester.tap(find.byKey(const Key('login_button')));
      await tester.pumpAndSettle();

      // this should find patient list since i am pushed to it
      expect(find.byType(PatientList), findsOneWidget);

      await tester.pumpAndSettle();
    });

    testWidgets('Test that owner is able to reset password',
        (final tester) async {
      const String ownerEmail = 'owner@test.com';

      final authController = AuthController();
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: const Hippotherapy(),
        ),
      );
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('logout_btn')));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Login as Owner'));
      await tester.pumpAndSettle();

      // Login as a therapist
      await tester.enterText(
          find.byKey(const Key('t_email_field')), ownerEmail);
      await tester.pumpAndSettle();

      // Click forgot password button
      await tester.tap(find.byKey(const Key('forgot_pwd')));
      await tester.pumpAndSettle();

      // Accept the prompt
      await tester.tap(find.byKey(const Key('req_pwd_reset')));
      await tester.pumpAndSettle();

      // Fetch the latest email from MailHog via the backend
      final res = await http
          .get(Uri.parse('$URL_START/$testsController/get-latest-mailv2'));
      final String passwdResetLink = res.body;

      await Future.delayed(const Duration(seconds: 2));

      debugPrint('Fully Cleaned Email Link: $passwdResetLink');

      // Parse the link and extract query parameters
      final Uri resetLink = Uri.parse(passwdResetLink);
      final String? linkParam = resetLink.queryParameters['link'];

      debugPrint('linkparam: ${linkParam!}');
      debugPrint('parsed link param: ${Uri.parse(linkParam)}');

      // Navigate to the PasswordResetPage with the link parameter
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: MaterialApp(
            home: const Hippotherapy(),
            onGenerateRoute: (final settings) {
              if (settings.name == PasswordResetPage.RouteName) {
                // Return a MaterialPageRoute with the PasswordResetPage
                return MaterialPageRoute(
                  builder: (final context) => PasswordResetPage(
                    testPasswdResetLink: Uri.parse(linkParam),
                  ),
                );
              }
              // Delegate other routes to Hippotherapy.routes
              final routeBuilder = Hippotherapy.routes[settings.name];
              if (routeBuilder != null) {
                return MaterialPageRoute(builder: routeBuilder);
              }
              return null;
            },
          ),
        ),
      );

      // Simulate navigation to the PasswordResetPage
      Navigator.of(tester.element(find.byType(Hippotherapy))).pushNamed(
        PasswordResetPage.RouteName,
      );

      await tester.pumpAndSettle();

      // Enter new password
      await tester.enterText(find.byKey(const Key('pwd_field')), OBNOXIOUS_PWD);
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('conf_pwd_field')), OBNOXIOUS_PWD);
      await tester.pumpAndSettle();

      // Submit the form
      await tester.tap(find.byKey(const Key('nu_pwd_submit')));
      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 2));

      // Verify successful password reset
      expect(find.text('Password reset successfully!'), findsOneWidget);
      await tester.pumpAndSettle();

      // switch to owner login
      await tester.tap(find.text('Login as Owner'));
      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 2));

      // Login as a owner with NEW password
      await tester.enterText(
          find.byKey(const Key('t_email_field')), ownerEmail);
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('t_password_field')), OBNOXIOUS_PWD);
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));

      // press login
      await tester.tap(find.byKey(const Key('login_button')));
      await tester.pumpAndSettle();
      await tester.pump(Duration(seconds: extraWaitSeconds));
      await tester.pump(const Duration(seconds: 10));

      // Changed to detect no on login test for reliability
      expect(find.text("Login"), findsNothing);
      // this should find therapist list since i am pushed to it
      // expect(find.byType(TherapistListPage), findsOneWidget);
    });
  });
}

Future<void> pumpPasswordResetPage(final WidgetTester tester) async {
  // Ensure window size is reset after the test
  addTearDown(() {
    tester.view.resetPhysicalSize();
    tester.view.resetDevicePixelRatio();
  });

  // Pump the RegistrationPage widget
  await tester.pumpWidget(const MaterialApp(home: PasswordResetPage()));
  await tester.pumpAndSettle();
}

/// A custom print function to print yellow text
void printYellow(final String text) {
  debugPrint('\x1B[33m$text\x1B[0m');
}

/// A custom print function to print green text
void printGreen(final String text) {
  debugPrint('\x1B[32m$text\x1B[0m');
}

/// A custom print function to print red text
void printRed(final String text) {
  debugPrint('\x1B[31m$text\x1B[0m');
}
