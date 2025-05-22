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

  group('end-to-end test', () {
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

      await Future.delayed(const Duration(seconds: 5));

      response = await http.post(Uri.parse(
          'http://localhost:5001/integration-tests/seed-cached-evaluation-data'));
      expect(response.statusCode, 200);
    });

    /// This test will rely on the cached evaluation being in the database, it should
    /// seed it itself, but it will not seed it into the live-firebase without
    /// first doing setup in the
    /// backend_emulator/integration-tests/AuthorizationTests.cs
    /// file
    testWidgets(
        'Expect that the values that were set before persist throughout a new session',
        (final tester) async {
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: AuthController(),
          child: const MaterialApp(
            home: Hippotherapy(),
          ),
        ),
      );

      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      // login as a therapist
      await tester.enterText(
          find.byKey(const Key('t_email_field')), 'johnsmith1@test.com');
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('t_password_field')), 'Password1!');
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('login_button')));
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      // need awaits since sessions do not show up
      await Future.delayed(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      // click patient name
      await tester.pumpAndSettle(const Duration(seconds: 2));
      await tester.tap(find.text('John Smith'));
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      await Future.delayed(const Duration(seconds: 2));

      await tester.pumpAndSettle(const Duration(seconds: 2));
      await tester.tap(find.text("2020-12-31\t\t\t\t\t4:20"));
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      await tester.pumpAndSettle(const Duration(seconds: 2));
      expect(find.text("Pre Evaluation: In Progress"), findsOneWidget);

      await tester.tap(find.text("Pre Evaluation: In Progress"));
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();
      await tester.pumpAndSettle();

      await tester.pumpAndSettle(const Duration(seconds: 2));
      await tester.tap(find.text("Thorax")); // HEAD TAB
      await tester.pumpAndSettle();

      await tester.pumpAndSettle(const Duration(seconds: 2));
      // these are the form's options for the neck and lumbar option chips
      final op1 =
          tester.widget(find.byKey(Key(PoseType.THORACIC_NEG_TWO.toString())));
      expect((op1 as FormBuilderChipOption).value, -2);

      final op2 =
          tester.widget(find.byKey(Key(PoseType.LUMBER_POS_TWO.toString())));
      expect((op2 as FormBuilderChipOption).value, 2);
    });
  });
}
