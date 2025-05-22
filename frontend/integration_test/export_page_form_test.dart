import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/main.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:provider/provider.dart';

const String exportTabName = 'Export';
const String owner1Email = "owner1@test.com";
const String owner2Email = "owner2@test.com";
const String owner3NoDataEmail = "owner3@test.com";
const String defaultPassword = "Password1!";

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('end-to-end test', () {
    setUpAll(() async {
      String url = ServerConfig.getClearEmulatorDataRoute();
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getClearEmulatorAuthRoute();
      response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = ServerConfig.getSeedExportPageData();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 15));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      await Future.delayed(const Duration(seconds: 3));
    });

    testWidgets('all widgets on form display correctly ', (final tester) async {
      await gotToExportTab(tester, needsLogin: true);

      expect(find.text('Name Of Patient'), findsOneWidget);
      expect(find.text('Date Range'), findsOneWidget);
      expect(find.text('Condition'), findsOneWidget);
      expect(find.text('Location'), findsOneWidget);
      expect(find.text('Export All Fields'), findsOneWidget);
      expect(find.text('Submit'), findsOneWidget);

      // verify the checkbox is true on default
      final checkboxWidget = tester
          .widget<CheckboxListTile>(find.byKey(const Key('exportAllBox')));
      expect(checkboxWidget.value, true);
    });

    testWidgets('Selects patient name and checks if export all button is false',
            (final tester) async {
          // Load app widget.
          await gotToExportTab(tester);

          // taps the checkbox
          final nameFieldBox = find.byKey(const Key('nameOfPatientBox'));
          await tester.tap(nameFieldBox);
          await tester.pumpAndSettle();

          final nameBox = tester.widget<Checkbox>(nameFieldBox);
          expect(nameBox.value, true);

          final nameField = find.byKey(const Key('nameOfPatient'));
          await tester.tap(nameField);
          await tester.pumpAndSettle();
          // await Future.delayed(Duration(seconds: 2)); // have to wait for it to render

          expect(find.byKey(const Key("Alice Tailor")), findsOne);
          expect(find.byKey(const Key("John Smith")), findsOne);

          // tap john
          await tester.tap(find.byKey(const Key("John Smith")));
          await tester.pumpAndSettle();

          expect(find.text("Alice Tailor"), findsNothing);
          expect(find.text("John Smith"), findsOneWidget);

          // check export all button is unchcked
          final checkboxWidget = tester
              .widget<CheckboxListTile>(find.byKey(const Key('exportAllBox')));
          expect(checkboxWidget.value, false);
        });

    testWidgets('selects Range of date and checks export all fields is false',
            (final tester) async {
          // Load app widget.
          await gotToExportTab(tester);

          // selects the checkbox
          final rangeOfDateBox = find.byKey(const Key('dateNowBox'));
          await tester.tap(rangeOfDateBox);
          await tester.pumpAndSettle();

          // checks if rangeOfDate
          final dateBox = tester.widget<Checkbox>(rangeOfDateBox);
          expect(dateBox.value, true);

          // selects the date range field
          final rangeOfDate = find.byKey(const Key('dateNow'));
          await tester.tap(rangeOfDate);
          await tester.pumpAndSettle();

          final DateTime today = DateTime.now();
          await tester.tap(find.text(today.day.toString()));
          await tester.pumpAndSettle();

          await tester.tap(find.text(today.day.toString()));
          await tester.pumpAndSettle();

          await tester.tap(find.text("Save"));
          await tester.pumpAndSettle();

          expect(
              find.text(
                  "${today.month}/${today.day}/${today.year} - ${today.month}/${today.day}/${today.year}"),
              findsOneWidget);

          // verify the checkbox is false
          final checkboxWidget = tester
              .widget<CheckboxListTile>(find.byKey(const Key('exportAllBox')));
          expect(checkboxWidget.value, false);
        });

    testWidgets('selects condition and export all is false',
            (final tester) async {
          // Load app widget.
          await gotToExportTab(tester);

          // selects the checkbox
          final conditionBox = find.byKey(const Key('conditionBox'));
          await tester.tap(conditionBox);
          await tester.pumpAndSettle();

          final conditionCheck = tester.widget<Checkbox>(conditionBox);
          expect(conditionCheck.value, true);

          final conditionField = find.byKey(const Key('condition'));
          await tester.tap(conditionField);
          await tester.pumpAndSettle();

          expect(find.text('Celebral Palsy'), findsOneWidget);
          expect(find.text('Autism'), findsOneWidget);
          expect(find.text('Paralysis'), findsNothing);
          expect(find.text('Down Syndrome'), findsNothing);

          await tester.tap(find.text('Celebral Palsy'));
          await tester.pumpAndSettle();

          expect(find.text('Celebral Palsy'), findsOneWidget);
          expect(find.text('Autism'), findsNothing);

          // verify the checkbox is false
          final checkboxWidget = tester
              .widget<CheckboxListTile>(find.byKey(const Key('exportAllBox')));
          expect(checkboxWidget.value, false);
        });

    testWidgets('selects location and export all field is false',
            (final tester) async {
          // Load app widget.
          await gotToExportTab(tester);

          // selects the locationBox
          final locationBox = find.byKey(const Key('locationBox'));
          await tester.tap(locationBox);
          await tester.pumpAndSettle();

          final locationCheck = tester.widget<Checkbox>(locationBox);
          expect(locationCheck.value, true);

          // selects the location field
          final location = find.byKey(const Key('location'));
          await tester.tap(location);
          await tester.pumpAndSettle();

          expect(find.text('LA'), findsOneWidget);
          expect(find.text('NA'), findsOneWidget);
          expect(find.text('CA'), findsOneWidget);
          expect(find.text('America'), findsNothing);

          await tester.tap(find.text('NA'));
          await tester.pumpAndSettle();

          expect(find.text('NA'), findsOneWidget);
          expect(find.text('LA'), findsNothing);

          // verify the checkbox is false
          final checkboxWidget = tester
              .widget<CheckboxListTile>(find.byKey(const Key('exportAllBox')));
          expect(checkboxWidget.value, false);
        });

    testWidgets(
        'full walkthrough of submits form by inputting all the information',
            (final tester) async {
          // Load app widget.
          await gotToExportTab(tester);

          // tapping the name field
          await selectNameField(tester, "John Smith");

          // tapping the condition
          await selectConditionField(tester, "Autism");

          // tapping the location
          await selectLocationBox(tester, "NA");

          final submitButton = find.text("Submit");
          await tester.tap(submitButton);
          await tester.pumpAndSettle();

          expect(find.text("Export Successful"), findsOneWidget);

          await tester.tap(find.text("OK"));
          await tester.pumpAndSettle();
        });

    testWidgets(
        'Export All Checkbox is checked and all other checkboxes are unchecked',
            (final tester) async {
          // tapping the name field
          await gotToExportTab(tester);
          await selectNameField(tester, "John Smith");

          // tapping the condition
          await selectConditionField(tester, "Autism");

          // tapping the location
          await selectLocationBox(tester, "NA");

          await selectDateField(tester, "1", "1");

          await tester.tap(find.byKey(const Key('exportAllBox')));
          await tester.pumpAndSettle();

          final checkboxWidget = tester
              .widget<CheckboxListTile>(find.byKey(const Key('exportAllBox')));
          expect(checkboxWidget.value, true);

          final nameBox =
          tester.widget<Checkbox>(find.byKey(const Key('nameOfPatientBox')));
          expect(nameBox.value, false);
          final dateBox =
          tester.widget<Checkbox>(find.byKey(const Key('dateNowBox')));
          expect(dateBox.value, false);
          final conditionBox =
          tester.widget<Checkbox>(find.byKey(const Key('conditionBox')));
          expect(conditionBox.value, false);
          final locationBox =
          tester.widget<Checkbox>(find.byKey(const Key('locationBox')));
          expect(locationBox.value, false);
        });

    testWidgets(
        'Export All Checkbox is unchecked and other checkboxes are also unchecked and submitted',
            (final tester) async {
          await gotToExportTab(tester);

          await tester.tap(find.byKey(const Key('exportAllBox')));
          await tester.pumpAndSettle();

          // turn it on
          //await tester.tap(find.byKey(const Key("dateNowBox")));
          //await tester.pumpAndSettle();

          // trun it off again
          //await tester.tap(find.byKey(const Key("dateNowBox")));
          //await tester.pumpAndSettle();

          final submitButton = find.text("Submit");
          await tester.tap(submitButton);
          await tester.pumpAndSettle();

          expect(find.text("Export Successful"), findsOneWidget);

          await tester.tap(find.text("OK"));
          await tester.pumpAndSettle();
        });

    testWidgets('submits form but with no values found', (final tester) async {
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

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('logout_btn')));
      await tester.pumpAndSettle();

      await gotToExportTab(tester, email: owner3NoDataEmail, needsLogin: true);

      await selectDateField(tester, "1", "1");

      final submitButton = find.text("Submit");
      await tester.tap(submitButton);
      await tester.pumpAndSettle();

      expect(
          find.text("Export Unsuccessful, no records found."), findsOneWidget);

      await tester.tap(find.text("OK"));
      await tester.pumpAndSettle();
    });
  });
}

Future<void> gotToExportTab(final WidgetTester tester,
    {final String email = owner1Email, final bool needsLogin = false}) async {
  await tester.binding
      .setSurfaceSize(const Size(1000, 1000)); // setting the window size
  await tester.pumpAndSettle();

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
    // login as a owner
    await tester.tap(find.text('Login as Owner'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byKey(const Key('t_email_field')), email);
    await tester.pumpAndSettle();

    await tester.enterText(
        find.byKey(const Key('t_password_field')), defaultPassword);
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('login_button')));
    await tester.pumpAndSettle();
  }

  // Open menu
  final menuButton = find.byIcon(Icons.menu);
  await tester.tap(menuButton);
  await tester.pumpAndSettle();

  // Click patients link
  await tester.tap(find.text(exportTabName));
  await tester.pumpAndSettle();
}

Future<void> selectNameField(
    final WidgetTester tester, final String name) async {
  final nameFieldBox = find.byKey(const Key('nameOfPatientBox'));
  await tester.tap(nameFieldBox);
  await tester.pumpAndSettle();

  final nameBox = tester.widget<Checkbox>(nameFieldBox);
  expect(nameBox.value, true);

  final nameField = find.byKey(const Key('nameOfPatient'));
  await tester.tap(nameField);
  await tester.pumpAndSettle();

  await tester.tap(find.byKey(Key(name)));
  await tester.pumpAndSettle();
}

Future<void> selectDateField(final WidgetTester tester, final String fromDate,
    final String toDate) async {
  await tester.tap(find.byKey(const Key('dateNowBox')));
  await tester.pumpAndSettle();

  await tester.tap(find.byKey(const Key('dateNow')));
  await tester.pumpAndSettle();

  await tester.tap(find.text(fromDate).last.last);
  await tester.pumpAndSettle();

  await tester.tap(find.text(toDate).last.last);
  await tester.pumpAndSettle();

  await tester.tap(find.text("Save"));
  await tester.pumpAndSettle();
}

Future<void> selectConditionField(
    final WidgetTester tester, final String condition) async {
  // Wait for any ongoing animations to complete
  await tester.pumpAndSettle();

  // ensure the condition box is visible
  final conditionBox = find.byKey(const Key('conditionBox'));
  await tester.ensureVisible(conditionBox);
  await tester.tap(conditionBox);
  await tester.pumpAndSettle();

  // ensure the condition field is visible
  final conditionField = find.byKey(const Key('condition'));
  await tester.ensureVisible(conditionField);
  await tester.tap(conditionField);
  await tester.pumpAndSettle();

  // Ensure the condition text is visible and tap it
  final conditionText = find.text(condition);
  await tester.ensureVisible(conditionText);
  await tester.tap(conditionText);
  await tester.pumpAndSettle();
}

Future<void> selectLocationBox(
    final WidgetTester tester, final String location) async {
  final locationBox = find.byKey(const Key('locationBox'));
  await tester.tap(locationBox);
  await tester.pumpAndSettle();

  // selects the location field
  final locationField = find.byKey(const Key('location'));
  await tester.tap(locationField);
  await tester.pumpAndSettle();

  await tester.tap(find.text(location));
  await tester.pumpAndSettle();
}
