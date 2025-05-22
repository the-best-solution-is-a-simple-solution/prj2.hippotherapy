import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/pages/login_page.dart';
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  late String uniqueEmail;

  Future<void> pumpHomePage(final WidgetTester tester) async {
    final authController = AuthController();
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthController>.value(
        value: authController,
        child: const MaterialApp(
          home: LoginPage(title: "Hippotherapy"),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<void> fillLoginForm(final WidgetTester tester,
      {final String email = 'test@example.com',
      final String password = 'Password123!'}) async {
    await tester.enterText(find.byKey(const Key("t_email_field")), email);
    await tester.enterText(find.byKey(const Key('t_password_field')), password);
    await tester.pumpAndSettle();
  }

  Future<void> tapLoginButton(final WidgetTester tester) async {
    await tester.tap(find.byKey(const ValueKey('login_button')));
    await tester.pumpAndSettle();
  }

  group('Login Tests', () {
    testWidgets('Login with invalid email format', (final tester) async {
      await pumpHomePage(tester);

      // Enter invalid email
      await fillLoginForm(tester, email: 'invalid-email');

      // Use the key to tap the login button
      await tester.tap(find.byKey(const ValueKey('login_button')));
      await tester.pumpAndSettle();

      // Check for error message
      expect(find.text('Please enter a valid email'), findsOneWidget);
    });

    // Reading snackbar isn't working in tests
    testWidgets('Failed login with incorrect password',
        (final WidgetTester tester) async {
      // Navigate to the Home Page
      await pumpHomePage(tester);

      // Fill the login form with an existing email and wrong password
      await fillLoginForm(
        tester,
        email: 'existinguser@example.com',
        password: 'WrongPassword',
      );

      // Tap the login button
      await tapLoginButton(tester);

      // Allow the SnackBar animation to start
      await tester.pump();

      // Allow the SnackBar time to appear
      await tester.pump(const Duration(seconds: 1));

      // Find the SnackBar
      final snackBarFinder = find.byType(SnackBar);
      expect(snackBarFinder, findsOneWidget);

      // Verify the SnackBar contains the expected error message
      expect(
        find.descendant(
          of: snackBarFinder,
          matching: find.text('Invalid email or password.'),
        ),
        findsOneWidget,
      );
    });

    testWidgets('Login with empty email field', (final tester) async {
      await pumpHomePage(tester);

      await fillLoginForm(tester, email: '', password: 'CorrectPassword123!');
      await tapLoginButton(tester);

      // Expect error for empty email
      expect(find.text('Email is required'), findsOneWidget);
    });

    testWidgets('Login with empty password field', (final tester) async {
      await pumpHomePage(tester);

      await fillLoginForm(tester,
          email: 'existinguser@example.com', password: '');
      await tapLoginButton(tester);

      // Expect error for empty password
      expect(find.text('Password is required'), findsOneWidget);
    });

    testWidgets('Login with invalid email format', (final tester) async {
      await pumpHomePage(tester);

      await fillLoginForm(tester,
          email: 'invalid-email-format', password: 'CorrectPassword123!');
      await tapLoginButton(tester);

      // Check for validation error message
      expect(find.text('Please enter a valid email'), findsOneWidget);
    });
  });
}
