import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/pages/patient_list_page.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

final authController = AuthController();

///Test patient list
Future<void> main() async {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  const String patientsLinkName = 'Patients';
  const String patient1 = 'Aabha Singh';
  const String patient2 = 'Zebideer Russ';

  group('Testing patients displayed', () {
    setUpAll(() async {
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getSeedPatientListPageRoute();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 15));
      expect(response.statusCode, 200);
      debugPrint('Status: ${response.statusCode} for $url');
    });

    testWidgets(
        'List of Patient Sessions displays when session tab in profile is clicked',
        (final WidgetTester tester) async {
      // Launch app and wait for it to open
      final auth = AuthController();
      // Inject the AuthController into the widget tree
      // Necessary for home page (for now)
      await tester.pumpWidget(
        ChangeNotifierProvider<AuthController>.value(
          value: auth,
          child: const MaterialApp(
            home: Hippotherapy(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // login as a therapist
      await tester.enterText(
          find.byKey(const Key('t_email_field')), 't1@test.com');
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('t_password_field')), 'Password1!');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('login_button')));
      await tester.pumpAndSettle();

      // Open menu
      final menuButton = find.byIcon(Icons.menu);
      await tester.tap(menuButton);
      await tester.pumpAndSettle();

      // Click patients
      await tester.tap(find.text(patientsLinkName));
      await tester.pumpAndSettle();

      expect(find.byType(PatientList), findsOneWidget);
      await tester.pumpAndSettle();
      // Check the first and last patient names
      expect(find.text(patient1), findsOne);
      expect(find.text(patient2), findsOne);
    });
  });
}
