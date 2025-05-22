import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/registration_page.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

import 'patients_list_page_test.dart';

const String URL_START = '${ServerConfig.address}:${ServerConfig.port}';
const String testsController = ServerConfig.integrationTestsRoute;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('Registration Page Integration Tests', () {
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
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 15));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      await Future.delayed(const Duration(seconds: 3));
    });

    Future<void> fillLoginForm(final WidgetTester tester,
        {final String email = 'owner@test.com',
        final String password = 'Password1!',
        final bool role = false}) async {
      final authController = AuthController();

      // Inject the AuthController into the widget tree
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

      // if role is true, will sign in as owner, otherwise sign in as therapist
      if (role) {
        await tester.tap(find.text("Login as Owner"));
        await tester.pumpAndSettle();
      }

      await tester.enterText(find.byType(TextFormField).at(0), email);
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextFormField).at(1), password);
      await tester.pumpAndSettle();

      await tester.tap(find.text("Login"));
      await tester.pumpAndSettle();
    }

    testWidgets("Owner generating a referral code", (final tester) async {
      await fillLoginForm(tester, role: true); // sign in as owner
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Referral"));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key("referral_email")), "fakeEmail@mail.com");
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("submit_referral")));
      await tester.pumpAndSettle();

      expect(
          find.textContaining(
              "Referral successfully sent to fakeEmail@mail.com"),
          findsAny);

      await tester.tap(find.text("OK"));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Logout"));
      await tester.pumpAndSettle();
    });

    testWidgets("Owner generating a referral code with an invalid email",
        (final tester) async {
      await fillLoginForm(tester, role: true); // sign in as owner

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Referral"));
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key("referral_email")), "fakeEmail@mail");
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("submit_referral")));
      await tester.pumpAndSettle();

      expect(find.text("This field requires a valid email address."), findsOne);

      await tester.enterText(find.byKey(const Key("referral_email")), " ");
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("submit_referral")));
      await tester.pumpAndSettle();

      expect(find.text("Email field cannot be empty."), findsOne);

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Logout"));
      await tester.pumpAndSettle();
    });
    testWidgets("Therapist inputting invalid referral code",
        (final tester) async {
      final referralResponse =
          await AuthController().generateReferral("12345", "test@mail.com");
      final List<dynamic> responseBody = jsonDecode(referralResponse!.body);

      // grab email
      final res = await http
          .get(Uri.parse('$URL_START/$testsController/get-latest-mailv2'));
      final String referralLink = res.body;

      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: MaterialApp(
            home: const Hippotherapy(),
            onGenerateRoute: (final settings) {
              if (settings.name == RegistrationPage.RouteName) {
                // Return a MaterialPageRoute with the PasswordResetPage
                final Uri uri = Uri.parse(referralLink);
                debugPrint(uri.toString());
                debugPrint(uri.queryParametersAll.toString());
                return MaterialPageRoute(
                  builder: (final context) => RegistrationPage(
                    code: uri.queryParameters['code'] ?? "",
                    ownerId: uri.queryParameters['owner'] ?? "",
                    email: uri.queryParameters['email'] ?? "",
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

      Navigator.of(tester.element(find.byType(Hippotherapy))).pushNamed(
        RegistrationPage.RouteName,
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));

      expect(find.text(responseBody[1]), findsOne);
      expect(find.text("test@mail.com"), findsOne);

      await tester.enterText(
          find.byKey(const ValueKey("referralField")), "123456");

      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password1!');
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'John');
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'Smith');
      await tester.pumpAndSettle();

      await tester.tap(find.text("Register"));
      await tester.pumpAndSettle();
    });

    testWidgets(
        "Therapist inputting referral code and get to registration screen and auto filled email",
        (final tester) async {
      final referralResponse =
          await AuthController().generateReferral("12345", "test@mail.com");
      final List<dynamic> responseBody = jsonDecode(referralResponse!.body);

      // grab email
      final res = await http
          .get(Uri.parse('$URL_START/$testsController/get-latest-mailv2'));
      final String referralLink = res.body;

      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: MaterialApp(
            home: const Hippotherapy(),
            onGenerateRoute: (final settings) {
              if (settings.name == RegistrationPage.RouteName) {
                // Return a MaterialPageRoute with the PasswordResetPage
                final Uri uri = Uri.parse(referralLink);
                debugPrint(uri.toString());
                debugPrint(uri.queryParametersAll.toString());
                return MaterialPageRoute(
                  builder: (final context) => RegistrationPage(
                    code: uri.queryParameters['code'] ?? "",
                    ownerId: uri.queryParameters['owner'] ?? "",
                    email: uri.queryParameters['email'] ?? "",
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

      Navigator.of(tester.element(find.byType(Hippotherapy))).pushNamed(
        RegistrationPage.RouteName,
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));

      expect(find.text(responseBody[1]), findsOne);
      expect(find.text("test@mail.com"), findsOne);

      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password1!');
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'John');
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'Smith');
      await tester.pumpAndSettle();

      await tester.tap(find.text("Register"));
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 5));
    });

    testWidgets(
        "Therapist inputting referral code and get to registration screen and submits with different email provided.",
        (final tester) async {
      final referralResponse = await AuthController()
          .generateReferral("12345", "billybob@gmail.com");
      final List<dynamic> responseBody = jsonDecode(referralResponse!.body);

      // grab email
      final res = await http
          .get(Uri.parse('$URL_START/$testsController/get-latest-mailv2'));
      final String referralLink = res.body;

      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: authController,
          child: MaterialApp(
            home: const Hippotherapy(),
            onGenerateRoute: (final settings) {
              if (settings.name == RegistrationPage.RouteName) {
                // Return a MaterialPageRoute with the PasswordResetPage
                final Uri uri = Uri.parse(referralLink);
                debugPrint(uri.toString());
                debugPrint(uri.queryParametersAll.toString());
                return MaterialPageRoute(
                  builder: (final context) => RegistrationPage(
                    code: uri.queryParameters['code'] ?? "",
                    ownerId: uri.queryParameters['owner'] ?? "",
                    email: uri.queryParameters['email'] ?? "",
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

      Navigator.of(tester.element(find.byType(Hippotherapy))).pushNamed(
        RegistrationPage.RouteName,
      );

      await tester.pumpAndSettle(const Duration(seconds: 2));

      await tester.enterText(
          find.byKey(const ValueKey('emailField')), "NOTBILLYBOB@gmail.com");
      await tester.enterText(
          find.byKey(const ValueKey('passwordField')), 'Password1!');
      await tester.enterText(find.byKey(const ValueKey('fNameField')), 'John');
      await tester.enterText(find.byKey(const ValueKey('lNameField')), 'Smith');

      await tester.tap(find.text("Register"));
      await tester.pumpAndSettle();
    });
  });
}
