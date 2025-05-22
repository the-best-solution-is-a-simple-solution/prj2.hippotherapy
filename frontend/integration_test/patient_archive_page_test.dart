import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/widgets/archive_widgets/patient_archive_widget.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  final int extraWaitSeconds = 2;

  Future<void> seedArchiveData() async {
    debugPrint('Seeding archive data');
    final String seedUrl = ServerConfig.getSeedArchiveDataRoute();
    final seedResponse = await http.post(Uri.parse(seedUrl));
    debugPrint(
        'Seed response: ${seedResponse.statusCode} - ${seedResponse.body}');

    if (seedResponse.statusCode != 200) {
      debugPrint('Seed failed, but continuing tests');
    } else {
      debugPrint('Seed succeeded');
    }
  }

  Future<void> clearPatients() async {
    String url = ServerConfig.getClearEmulatorDataRoute();
    var response = await http.delete(Uri.parse(url));
    debugPrint('Status: ${response.statusCode} for $url');
    expect(response.statusCode, 200);

    url = ServerConfig.getClearEmulatorAuthRoute();
    response = await http.delete(Uri.parse(url)).timeout(const Duration(seconds: 15));
    debugPrint('Status: ${response.statusCode} for $url');
    expect(response.statusCode, 200);
  }

  setUpAll(() async {
    debugPrint('Starting setUpAll');
    await clearPatients();
    await seedArchiveData();
    debugPrint('setUpAll completed');
  });

  Future<void> clearWidgetTree(final WidgetTester tester) async {
    await tester.pumpWidget(Container());
  }

  Future<void> goToPatientArchive(final WidgetTester tester) async {
    await clearWidgetTree(tester);

    final authController = AuthController();
    await authController.initialize();
    await authController.logout(); // Ensure fresh state

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthController>.value(
        value: authController,
        child: const Hippotherapy(),
      ),
    );
    await tester.pumpAndSettle();

    // Verify login page is present
    final emailField = find.byKey(const Key('t_email_field'));
    if (emailField.evaluate().isEmpty) {
      debugPrint('Email field not found!');
      throw Exception('Login page not rendered');
    }

    await tester.enterText(emailField, 'archive@test.com');
    await tester.pumpAndSettle();
    await tester.enterText(
        find.byKey(const Key('t_password_field')), 'Password1!');
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('login_button')));
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    await tester.tap(find.byIcon(Icons.menu));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Archive'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Patients'));
    await tester.pumpAndSettle();
  }

  Future<void> goToPatientList(
      final WidgetTester tester, final AuthController authController) async {
    await clearWidgetTree(tester);

    await tester.pumpWidget(
      ChangeNotifierProvider<AuthController>.value(
        value: authController,
        child: const Hippotherapy(),
      ),
    );
    await tester.pumpAndSettle();

    await tester.enterText(
        find.byKey(const Key('t_email_field')), 'archive@test.com');
    await tester.pumpAndSettle();
    await tester.enterText(
        find.byKey(const Key('t_password_field')), 'Password1!');
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('login_button')));
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));
  }

  testWidgets('test that deleting from patient archive removes from list',
      (final tester) async {
    await goToPatientArchive(tester);
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    final bobFinder = find.text('Bob Boberton');
    expect(bobFinder, findsOneWidget,
        reason: 'Bob Boberton should be in the archive');

        final deleteButton = find.descendant(
          of: find.ancestor(of: bobFinder, matching: find.byType(PatientArchiveWidget)),
          matching: find.byIcon(Icons.delete_forever),
        );
        expect(deleteButton, findsOneWidget,
            reason: 'Delete button should be present for Bob Boberton');

    await tester.tap(deleteButton);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Delete'));
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    expect(find.text('Bob Boberton has been permanently deleted.'),
        findsOneWidget);
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    expect(bobFinder, findsNothing);
  });

  testWidgets('test that archiving from patient list adds to archive',
      (final tester) async {
    final authController = AuthController();
    await authController.initialize();
    await authController.logout(); // Start fresh

    // Go to patient list and archive Amy
    await goToPatientList(tester, authController);
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    final amyFinder = find.text('Amy Adamson');
    expect(amyFinder, findsOneWidget,
        reason: 'Amy Adamson should be in patient list');

    final archiveButton = find.descendant(
      of: find.ancestor(of: amyFinder, matching: find.byType(ListTile)),
      matching: find.byIcon(Icons.archive),
    );
    expect(archiveButton, findsOneWidget,
        reason: 'Archive button should be present for Amy Adamson');

    await tester.tap(archiveButton);
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    final confirmButton = find.text('Yes');
    expect(confirmButton, findsOneWidget,
        reason: 'Confirmation "Yes" button should appear');
    await tester.tap(confirmButton);
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    expect(amyFinder, findsNothing,
        reason: 'Amy Adamson should be removed from patient list');

    // Navigate to archive without resetting the app
    await tester.tap(find.byIcon(Icons.menu));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Archive'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Patients'));
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    expect(amyFinder, findsOneWidget,
        reason: 'Amy Adamson should appear in archive');
  });

  testWidgets(
      'test that restoring patient in archive removes from list and adds to patient list',
      (final tester) async {
    await goToPatientArchive(tester);
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));
    await tester.pump(const Duration(seconds: 10)); // very important

    final billFinder = find.text('Bill Billington');
    expect(billFinder, findsOneWidget,
        reason: 'Bill Billington should be in the archive');

    final restoreButton = find.byKey(const Key('restore_archive-patient-3'));
    expect(restoreButton, findsOneWidget,
        reason: 'Restore button should be present for Bill Billington');

    await tester.tap(restoreButton);
    await tester.pumpAndSettle();
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    expect(billFinder, findsNothing,
        reason: 'Bill Billington should be removed from archive');

    await tester.tap(find.byIcon(Icons.menu));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Patients'));
    await tester.pumpAndSettle();
    await tester.pump(Duration(seconds: extraWaitSeconds));

    expect(billFinder, findsOneWidget,
        reason: 'Bill Billington should appear in patient list');
  });
}
