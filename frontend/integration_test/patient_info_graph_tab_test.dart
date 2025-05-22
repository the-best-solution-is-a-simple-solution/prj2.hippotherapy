import 'package:flutter/material.dart';
import 'package:flutter_date_range_picker/flutter_date_range_picker.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';
import 'package:syncfusion_flutter_charts/charts.dart';

///Test patient info page
Future<void> main() async {
  const String patientsLinkName = 'Patients';
  const String patientInfoSessionsTabName = 'Sessions';
  const String patientInfoGraphTabName = 'Graph';

  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  // Patient test data for session tab
  const String patientWithSessionsJohn = 'John Smith';
  const String patientWithNoSessionsAlice = 'Alice Tailor';
  const String sessionWithPrePost = '2022-12-31';
  const String sessionWithPost = '2022-02-02';
  const String sessionWithPre = '2022-01-10';
  const String sessionWithNoEvals = '2023-12-31';

  // Patient test data for graph tab
  const String patientAlbertNoEvals = 'Albert Noevals';
  const String patientAlbertOneEval = 'Albert Oneeval';
  const String patientAlbertSixsessions = 'Albert Sixsessions';
  const String graphTabAveragesToggleButton = 'Posture Averages';
  const String notEnoughGraphDataMessage = 'Not enough data for graph';
  const String noLinesSelectedErrorMessage = 'No lines selected';

  // graph tab Keys
  const Key dateRangeKey = Key('patientInfoPageGraphTabDateRangePickerKey');

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

  group('End-to-end tests for graph tab', () {
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

      url = ServerConfig.getSeedPatientInfoGraphTabRoute();
      response =
      await http.post(Uri.parse(url)).timeout(const Duration(seconds: 10));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);
    });

    testWidgets('graph tab exists and has controls',
            (final WidgetTester tester) async {
          await goToPatientInfo(tester, patientAlbertOneEval, needsLogin: true);
          // Should be Albert and graph tab should exist
          expect(find.text(patientAlbertOneEval), findsOneWidget);
          expect(find.text(patientInfoGraphTabName), findsOneWidget);

          // Click 'Graph' tab
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Check that controls are there
          expect(find.text(graphTabAveragesToggleButton), findsOneWidget);
          // Use find.byKey to locate the DateRangeField
          final dateRangeFieldFinder = find.byKey(dateRangeKey);
          expect(dateRangeFieldFinder, findsOneWidget);
          expect(find.textContaining('Found 1 evaluation'), findsOneWidget);
          expect(find.text('Select Postures'), findsOneWidget);
        });

    testWidgets('Therapist views patient evaluation graph with no data',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertNoEvals);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Check expected info messages
          expect(find.textContaining('Found 0 evaluations'), findsOneWidget);
          expect(find.text(notEnoughGraphDataMessage), findsOneWidget);

          // Check that controls are disabled
          final byTypeFinder = find.byType(DateRangeField);
          // Get the list of DateRangeField widgets
          final foundWidgets = tester.widgetList(byTypeFinder);
          expect(foundWidgets.length, 1);

          // Use find.byKey to locate the DateRangeField
          final dateRangeFieldFinder = find.byKey(dateRangeKey);
          expect(dateRangeFieldFinder, findsOneWidget);
          await tester.tap(dateRangeFieldFinder);
          await tester.pumpAndSettle();

          // check that date range did not appear
          expect(find.text('Confirm'), findsNothing);
          expect(find.text('All Evaluations'), findsNothing);
        });

    testWidgets(
        'Therapist views patient evaluation graph with only one evaluation',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertOneEval);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Find info text
          expect(find.textContaining('Found 1 evaluation'), findsOneWidget);
          expect(find.text(notEnoughGraphDataMessage), findsOneWidget);
        });

    testWidgets(
        'Therapist views patient evaluation graph with 2 or more patient evaluations',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // check info messages
          expect(find.textContaining('Found 11 evaluations'), findsOneWidget);

          // Check no postures selected by default but button works
          await tester.tap(find.text('Select Postures'));
          await tester.pumpAndSettle();
          expect(find.text('Head Ant'), findsOneWidget);
        });

    testWidgets('Select Posture button has all postures',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertOneEval);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Check no postures selected by default but button works
          await tester.tap(find.text('Select Postures'));
          await tester.pumpAndSettle();

          final posturesCheckboxes = tester
              .widgetList<CheckboxListTile>(find.byType(CheckboxListTile))
              .toList();

          expect(posturesCheckboxes.length >= 10, true);
          // Check that ALL widgets are NOT checked
          const bool checkedStatus = false;
          int index = 0;
          for (int i = 0; i < posturesCheckboxes.length; i++) {
            expect(posturesCheckboxes.elementAt(index++).value, checkedStatus);
          }

          expect(find.text('Head Ant'), findsOneWidget);
          expect(find.text('Head Lat'), findsOneWidget);
          expect(find.text('Elbow Extension'), findsOneWidget);
          expect(find.text('Hip Flex'), findsOneWidget);
          expect(find.text('Knee Flex'), findsOneWidget);
          expect(find.text('Lumbar'), findsOneWidget);
          expect(find.text('Pelvic'), findsOneWidget);
          expect(find.text('Thoracic'), findsOneWidget);
        });

    testWidgets('Date range opens and has default dates set',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Use find.byKey to locate the DateRangeField
          final dateRangeFieldFinder = find.byKey(dateRangeKey);
          expect(dateRangeFieldFinder, findsOneWidget);
          await tester.tap(dateRangeFieldFinder);
          await tester.pumpAndSettle();

          final selectedDateRange = tester
              .widgetList<DateRangeField>(find.byType(DateRangeField))
              .toList()
              .first;
          expect(selectedDateRange.selectedDateRange,
              DateRange(DateTime(2024, 1, 1), DateTime(2024, 5, 28)));

          // Check options for quick date range selection
          expect(find.text('All Evaluations'), findsOneWidget);
          expect(find.text('Last year before last session'), findsOneWidget);
          expect(find.text('90 days before last session'), findsOneWidget);
          expect(find.text('Cancel'), findsOneWidget);
          expect(find.text('Confirm'), findsOneWidget);
        });

    testWidgets('Therapist filters out data so 7 evaluations are shown by date',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Use find.byKey to locate the DateRangeField
          final dateRangeFieldFinder = find.byKey(dateRangeKey);
          expect(dateRangeFieldFinder, findsOneWidget);
          await tester.tap(dateRangeFieldFinder);
          await tester.pumpAndSettle();

          // change start date to include only 7 evaluations
          await tester.tap(find.text('90 days before last session'));
          await tester.pumpAndSettle();

          // Click OK
          await tester.tap(find.text('Confirm'));
          await tester.pumpAndSettle();

          expect(find.textContaining('Found 7 evaluations'), findsOneWidget);

          // Check data in filtered data object array
          final chart = tester
              .widgetList<SfCartesianChart>(find.byType(SfCartesianChart))
              .toList()
              .first;

          // 2 lines shown by default
          expect(chart.series.length, 2);
          expect(chart.series.first.dataSource?.length, 7);
        });

    testWidgets('Therapist selecting an invalid date displays no graph',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertOneEval);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Use find.byKey to locate the DateRangeField
          final dateRangeFieldFinder = find.byKey(dateRangeKey);
          expect(dateRangeFieldFinder, findsOneWidget);
          await tester.tap(dateRangeFieldFinder);
          await tester.pumpAndSettle();

          await tester.tap(find.text('1').first);
          await tester.pumpAndSettle();

          await tester.tap(find.text('2').first);
          await tester.pumpAndSettle();

          await tester.tap(find.text('Confirm').first);
          await tester.pumpAndSettle();

          expect(find.textContaining('Found 0 evaluations'), findsOneWidget);
          expect(find.text(notEnoughGraphDataMessage), findsOneWidget);
        });

    testWidgets('Therapist toggles the averages lines ',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Check data in filtered data object array
          final chart = tester
              .widgetList<SfCartesianChart>(find.byType(SfCartesianChart))
              .toList()
              .first;

          // 2 lines shown by default
          expect(chart.series.length, 2);
          expect(chart.series.first.dataSource?.length, 11);

          // Toggle off
          await tester.tap(find.text(graphTabAveragesToggleButton));
          await tester.pumpAndSettle();

          expect(find.text(noLinesSelectedErrorMessage), findsOneWidget);

          // Check data in filtered data object array
          expect(chart.series.length, 0);

          // Toggle on
          await tester.tap(find.text(graphTabAveragesToggleButton));
          await tester.pumpAndSettle();

          // Check data should be back
          expect(find.text(noLinesSelectedErrorMessage), findsNothing);
          expect(chart.series.length, 2);
          expect(chart.series.first.dataSource?.length, 11);
        });

    testWidgets('Therapist selects 1 posture to compare to the averages',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Click button
          await tester.tap(find.text('Select Postures'));
          await tester.pumpAndSettle();

          // Check one field
          await tester.tap(find.text('Head Lat'));
          await tester.pumpAndSettle();

          // Click back button
          await tester.tap(find.byIcon(Icons.arrow_back).at(1));
          await tester.pumpAndSettle();

          // Check data in filtered data object array
          final chart = tester
              .widgetList<SfCartesianChart>(find.byType(SfCartesianChart))
              .toList()
              .first;

          // 3 lines now shown
          expect(chart.series.length, 3);
        });

    testWidgets('Therapist selects all postures to display on graph',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Click button
          await tester.tap(find.text('Select Postures'));
          await tester.pumpAndSettle();

          // Click select all button
          final semanticFinder = find.bySemanticsLabel('Select/Deselect All');
          await tester.tap(semanticFinder);
          await tester.pumpAndSettle();

          final posturesCheckboxes = tester
              .widgetList<CheckboxListTile>(find.byType(CheckboxListTile))
              .toList();

          expect(posturesCheckboxes.length >= 10, true);
          // Check that ALL widgets are checked
          const bool checkedStatus = true;
          for (int i = 0; i < posturesCheckboxes.length; i++) {
            expect(posturesCheckboxes.elementAt(i).value, checkedStatus);
          }

          // Click back button
          await tester.tap(find.byIcon(Icons.arrow_back).at(1));
          await tester.pumpAndSettle();

          // Check data in filtered data object array
          final chart = tester
              .widgetList<SfCartesianChart>(find.byType(SfCartesianChart))
              .toList()
              .first;

          // 13 lines now shown
          expect(chart.series.length, 13);
        });

    testWidgets('Therapist deselects all poses',
            (final WidgetTester tester) async {
          // Click 'Graph' tab
          await goToPatientInfo(tester, patientAlbertSixsessions);
          await tester.tap(find.text(patientInfoGraphTabName));
          await tester.pumpAndSettle();

          // Click button
          await tester.tap(find.text('Select Postures'));
          await tester.pumpAndSettle();

          // Click select all button
          final semanticFinder = find.bySemanticsLabel('Select/Deselect All');
          await tester.tap(semanticFinder);
          await tester.pumpAndSettle();

          // Click select all button again to deselect all
          await tester.tap(semanticFinder);
          await tester.pumpAndSettle();

          // Check that ALL widgets are NOT checked
          final posturesCheckboxes = tester
              .widgetList<CheckboxListTile>(find.byType(CheckboxListTile))
              .toList();
          const bool checkedStatus = false;
          for (int i = 0; i < posturesCheckboxes.length; i++) {
            expect(posturesCheckboxes.elementAt(i).value, checkedStatus);
          }

          // Click back button
          await tester.tap(find.byIcon(Icons.arrow_back).at(1));
          await tester.pumpAndSettle();

          // Check data in filtered data object array
          final chart = tester
              .widgetList<SfCartesianChart>(find.byType(SfCartesianChart))
              .toList()
              .first;

          // 2 lines now shown
          expect(chart.series.length, 2);
        });
  });
}
