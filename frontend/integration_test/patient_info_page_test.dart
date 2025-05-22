import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:frontend/models/pose.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/evaluation_comparison_row_widget.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

///Test patient info page
Future<void> main() async {
  const String patientsLinkName = 'Patients';
  const String patientInfoSessionsTabName = 'Sessions';

  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // Patient test data for session tab
  const String patientWithSessionsJohn = 'John Smith';
  const String patientWithNoSessionsAlice = 'Alice Tailor';
  const String sessionWithPrePost = '2022-12-31\t\t\t\t\t0:00';
  const String sessionWithPost = '2022-02-02\t\t\t\t\t0:00';
  const String sessionWithPre = '2022-01-10\t\t\t\t\t0:00';
  const String sessionWithNoEvals = '2022-12-31\t\t\t\t\t0:00';

  Future<void> goToPatientInfo(
      final WidgetTester tester, final String patientName,
      {final bool needsLogin = false}) async {
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

    if (needsLogin) {
      // login as a therapist
      await tester.enterText(
          find.byKey(const Key('t_email_field')), 'info-therapist1@test.com');
      await tester.pumpAndSettle();

      await tester.enterText(
          find.byKey(const Key('t_password_field')), 'Password1!');
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('login_button')));
      await tester.pumpAndSettle();
    }

    // Open menu
    final menuButton = find.byIcon(Icons.menu);
    await tester.tap(menuButton);
    await tester.pumpAndSettle();

    // Click patients link
    await tester.tap(find.text(patientsLinkName));
    await tester.pumpAndSettle();

    // Click patient name
    await tester.tap(find.text(patientName));
    await tester.pumpAndSettle();
  }

  group('End-to-end tests for patient info page', () {
    setUpAll(() async {
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getSeedPatientInfoSessionTabRoute();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 15));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      await Future.delayed(const Duration(seconds: 10));
    });

    testWidgets('New Patient with no sessions selected',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithNoSessionsAlice,
          needsLogin: true);
      await tester.pumpAndSettle();

      // Click 'Sessions' tab
      await tester.tap(find.text(patientInfoSessionsTabName));
      await tester.pumpAndSettle();

      // Check sessions displayed
      expect(find.text('No sessions for patient'), findsOneWidget);
    });

    testWidgets('therapist changes emoji of patient',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("alicetailor-emoji-0")));
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();

      await tester.tap(find.text("🍇"));
      await tester.pumpAndSettle();

      expect(find.text("🍇"), findsAny);
    });

    testWidgets('patient info page opens when patient selected',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);

      // Patient info page with name and a tab named 'Sessions'
      expect(find.text(patientWithSessionsJohn), findsOneWidget);
      expect(find.text(patientInfoSessionsTabName), findsOneWidget);
    });

    testWidgets(
        'List of Patient Sessions displays when session tab in profile is clicked',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);

      expect(find.text(patientWithSessionsJohn), findsOneWidget);
      expect(find.text(patientInfoSessionsTabName), findsOneWidget);

      // Click 'Sessions' tab
      await tester.tap(find.text(patientInfoSessionsTabName));
      await tester.pumpAndSettle();

      // Check sessions displayed
      expect(find.text(sessionWithPrePost), findsOneWidget);
      expect(find.text(sessionWithPre), findsOneWidget);
      expect(find.text(sessionWithPost), findsOneWidget);
      expect(find.text(sessionWithNoEvals), findsOneWidget);
    });

    testWidgets('Session with Both pre and post evaluations clicked on',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);

      // Click 'Sessions' tab
      await tester.tap(find.text(patientInfoSessionsTabName));
      await tester.pumpAndSettle();

      // Check sessions displayed
      expect(find.text(sessionWithPrePost), findsOneWidget);

      await tester.tap(find.text(sessionWithPrePost));
      await tester.pumpAndSettle();

      await tester.tap(find.text('Comparisons'));
      await tester.pumpAndSettle();

      // Check comparison rows displayed
      expect(find.text('Hip Flex'), findsOneWidget);

      // Get the list of EvaluationComparisonRow widgets
      final byTypeFinder = find.byType(EvaluationComparisonRow);
      final foundWidgets = tester.widgetList(byTypeFinder);

      // Check that the found widgets are of the expected type
      final rows = foundWidgets.whereType<EvaluationComparisonRow>().toList();
      // Test specific values within the row
      const int hipFlexPos = 0;
      expect(rows.elementAt(hipFlexPos).rowTitle,
          PoseType.HIP_FLEX_NEUTRAL.category.displayName);
      expect(
          rows.elementAt(hipFlexPos).pose1, equals(PoseType.HIP_FLEX_NEUTRAL));
      expect(
          rows.elementAt(hipFlexPos).pose2, equals(PoseType.HIP_FLEX_NEUTRAL));
      const int kneeFlexPos = 4;
      expect(rows.elementAt(kneeFlexPos).rowTitle,
          PoseType.KNEE_FLEX_NEG_ONE.category.displayName);
      expect(rows.elementAt(kneeFlexPos).pose1,
          equals(PoseType.KNEE_FLEX_NEUTRAL));
      expect(rows.elementAt(kneeFlexPos).pose2,
          equals(PoseType.KNEE_FLEX_NEG_ONE));
    });

    testWidgets('test back button works', (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);
      await tester.pageBack();
      await tester.pumpAndSettle();

      expect(find.text(patientInfoSessionsTabName), findsNothing);
    });

    testWidgets('Sessions sorted by oldest date by default ',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);

      // Find the first visible session date text
      final RegExp dateRegEx = RegExp(r'\d{4}-\d{2}-\d{2}');
      final sessionDateFinder = find.byWidgetPredicate((final widget) =>
          widget is Text &&
          dateRegEx.hasMatch(widget.data ??
              '')); // If widget has no data put default '' to prevent null reference

      await tester.pump(const Duration(seconds: 1));

      // Get the first visible date text
      final firstVisibleDate =
          (tester.widget<Text>(sessionDateFinder.first)).data;
      expect(firstVisibleDate, equals('2022-12-31\t\t\t\t\t0:00'));
    });

    testWidgets('Sessions sorted by reversed on date sort button clicked ',
        (final WidgetTester tester) async {
      await goToPatientInfo(tester, patientWithSessionsJohn);

      await tester.tap(find.text(patientInfoSessionsTabName));
      await tester.pumpAndSettle();

      // Click date sort button
      await tester.tap(find.byKey(const Key('SessionsDateSortButton')));
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 1));

      // Find the first visible session date text
      final sessionDateFinder = find.byWidgetPredicate((final widget) =>
          widget is Text &&
          RegExp(r'\d{4}-\d{2}-\d{2}').hasMatch(widget.data ?? ''));

      // Get the first visible date text
      final firstVisibleDate =
          (tester.widget<Text>(sessionDateFinder.first)).data;
      expect(firstVisibleDate, equals('2020-12-31\t\t\t\t\t0:00'));
    });
  });
}
