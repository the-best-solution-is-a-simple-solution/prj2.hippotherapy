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

  Future<void> goToPatientInfo(final WidgetTester tester,
      final String patientName, final bool needLogout) async {
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

    if (needLogout) {
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.menu));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('logout_btn')));
      await tester.pumpAndSettle();
    } else {
      await tester.pumpAndSettle(const Duration(seconds: 5));
    }

    await tester.pumpAndSettle();

    // login as a therapist
    await tester.enterText(
        find.byKey(const Key('t_email_field')), 'johnsmith1@test.com');
    await tester.pumpAndSettle();

    await tester.enterText(
        find.byKey(const Key('t_password_field')), 'Password1!');
    await tester.pumpAndSettle();

    await tester.tap(find.byKey(const Key('login_button')));
    await tester.pumpAndSettle(const Duration(seconds: 3));

    // click patient name
    await tester.tap(find.text(patientName));
    await tester.pumpAndSettle();
  }

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

      url = ServerConfig.getSeedEvaluationPageData();
      response =
          await http.post(Uri.parse(url)).timeout(const Duration(seconds: 15));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      await Future.delayed(const Duration(seconds: 5));
    });

    testWidgets('checking if evaluation page renders', (final tester) async {
      await goToPatientInfo(tester, "John Smith", false);

      await Future.delayed(const Duration(seconds: 2));
      await tester.pump(const Duration(seconds: 2));

      await tester.tap(find.textContaining("2022-02-02"));
      await tester.pumpAndSettle();

      // selecting pre-evaluation button
      await tester.tap(find.text("Post Evaluation: Not Started"));
      await tester.pumpAndSettle();

      // accepts the create option from popup
      await tester.tap(find.text("Create"));
      await tester.pumpAndSettle();

      // checks if tab exists
      expect(find.text('Head'), findsOneWidget);
      expect(find.text('Arm'), findsOneWidget);
      expect(find.text('Thorax'), findsOneWidget);
      expect(find.text('Hip'), findsOneWidget);
      expect(find.text('Leg'), findsOneWidget);
      expect(find.text('Submit'), findsNothing);
    });

    testWidgets(
        'test that evaluation view renders and displays full evaluation',
        (final tester) async {
      // Load app widget.
      await goToPatientInfo(tester, "John Smith", true);

      await tester.pumpAndSettle();

      await tester.tap(find.textContaining(
          "2022-01-10")); // click the already existing date with one pre evaluation
      await tester.pumpAndSettle();

      await tester.tap(find.text('Pre Evaluation: Completed'));
      await tester.pumpAndSettle();

      // Scroll until the image is visible
      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.HIP_FLEX_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.HIP_FLEX_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.LUMBER_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.LUMBER_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.HEAD_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.HEAD_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.HEAD_ANT_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.HEAD_ANT_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.KNEE_FLEX_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.KNEE_FLEX_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.PELVIC_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.PELVIC_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.PELVIC_TILT_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.PELVIC_TILT_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.THORACIC_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.THORACIC_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.TRUNK_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.TRUNK_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.TRUNK_INCLINATION_NEUTRAL.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.TRUNK_INCLINATION_NEUTRAL.imgPath)),
          findsOneWidget);

      await tester.scrollUntilVisible(
        find.image(AssetImage(PoseType.ELBOW_EXTENSION_POS_ONE.imgPath)),
        150.0,
        scrollable: find.byType(Scrollable),
      );
      await tester.pumpAndSettle();
      expect(find.image(AssetImage(PoseType.ELBOW_EXTENSION_POS_ONE.imgPath)),
          findsOneWidget);
    });

    testWidgets(
        'creating a session, then an initial evaluation and completely filling out the form and successfully submit it',
        (final tester) async {
      // Load app widget.
      await goToPatientInfo(tester, "John Smith", true);

      await tester.tap(find
          .byTooltip("Add a new session for John Smith.")); // create a session
      await tester.pumpAndSettle();

      await tester.tap(find.text("Yes")); // select yes for prompt
      await tester.pumpAndSettle();

      // selecing pre-evaluation button
      await tester.tap(find.text("Pre Evaluation: Not Started"));
      await tester.pumpAndSettle();

      // accepts the create option from popup
      await tester.tap(find.text("Create"));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("HeadTab"))); // HEAD TAB
      await tester.pumpAndSettle();

      // HEAD
      await tester.tap(find.byKey(Key(PoseType.HEAD_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // HEAD ANT
      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEG_TWO.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Arm")); // ARM TAB
      await tester.pumpAndSettle();

      // ELBOW EXTENSION
      await tester
          .tap(find.byKey(Key(PoseType.ELBOW_EXTENSION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Thorax")); // THORAX TAB
      await tester.pumpAndSettle();

      // LUMBAR
      await tester.tap(find.byKey(Key(PoseType.LUMBER_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // THORAIC
      await tester.tap(find.byKey(Key(PoseType.THORACIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // check again to see that submit is still not visible
      expect(find.text('submit'), findsNothing);

      await tester.tap(find.text("Hip")); // HIP TAB
      await tester.pumpAndSettle();

      // HIP FLEX
      await tester.tap(find.byKey(Key(PoseType.HIP_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC
      await tester.tap(find.byKey(Key(PoseType.PELVIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC TILT
      await tester
          .tap(find.byKey(Key(PoseType.PELVIC_TILT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // TRUNK NEUTRAL
      await tester.tap(find.byKey(Key(PoseType.TRUNK_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.drag(find.byType(Scrollable),
          const Offset(0, -300)); // got to bottom of the screen
      await tester.pumpAndSettle();

      // TRUNK INCLINATION
      await tester
          .tap(find.byKey(Key(PoseType.TRUNK_INCLINATION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Leg")); // LEG TAB
      await tester.pumpAndSettle();

      // KNEE
      await tester.tap(find.byKey(Key(PoseType.KNEE_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // once everything is done, Submit should be visible, and selectable
      expect(find.text('Submit'), findsOneWidget);

      await tester.tap(find.text("Submit")); // submit button
      await tester.pumpAndSettle();

      await tester.pump(const Duration(seconds: 1));

      expect(find.text("Evaluation Submission Received"), findsOneWidget);

      await tester.tap(find.byKey(const Key("submitEvaluationYes")));
      await tester.pumpAndSettle();
    });

    testWidgets(
        'going into an already existing session, then an post evaluation and completely filling out the form and successfully submit it',
        (final tester) async {
      // Load app widget.
      await goToPatientInfo(tester, "John Smith", true);

      await tester.tap(find.textContaining("2022-01-10")); // click the already existing date with one pre evaluation
      await tester.pumpAndSettle();

      // selecing pre-evaluation button
      await tester.tap(find.text("Post Evaluation: Not Started"));
      await tester.pumpAndSettle();

      // accepts the create option from popup
      await tester.tap(find.text("Create"));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("HeadTab"))); // HEAD TAB
      await tester.pumpAndSettle();

      // HEAD
      await tester.tap(find.byKey(Key(PoseType.HEAD_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // HEAD ANT
      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEG_TWO.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Arm")); // ARM TAB
      await tester.pumpAndSettle();

      // ELBOW EXTENSION
      await tester
          .tap(find.byKey(Key(PoseType.ELBOW_EXTENSION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Thorax")); // THORAX TAB
      await tester.pumpAndSettle();

      // LUMBAR
      await tester.tap(find.byKey(Key(PoseType.LUMBER_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // THORAIC
      await tester.tap(find.byKey(Key(PoseType.THORACIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // check again to see that submit is still not visible
      expect(find.text('submit'), findsNothing);

      await tester.tap(find.text("Hip")); // HIP TAB
      await tester.pumpAndSettle();

      // HIP FLEX
      await tester.tap(find.byKey(Key(PoseType.HIP_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC
      await tester.tap(find.byKey(Key(PoseType.PELVIC_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // PELVIC TILT
      await tester
          .tap(find.byKey(Key(PoseType.PELVIC_TILT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // TRUNK NEUTRAL
      await tester.tap(find.byKey(Key(PoseType.TRUNK_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.drag(find.byType(Scrollable),
          const Offset(0, -300)); // got to bottom of the screen
      await tester.pumpAndSettle();

      // TRUNK INCLINATION
      await tester
          .tap(find.byKey(Key(PoseType.TRUNK_INCLINATION_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.text("Leg")); // LEG TAB
      await tester.pumpAndSettle();

      // KNEE
      await tester.tap(find.byKey(Key(PoseType.KNEE_FLEX_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // once everything is done, Submit should be visible, and selectable
      expect(find.text('Submit'), findsOneWidget);

      await tester.tap(find.text("Submit")); // submit button
      await tester.pumpAndSettle();
    });

    testWidgets(
        'while partially filling in an evaluation page, exit the application and reenter with the data previously inputted saved',
        (final tester) async {
      // Load app widget.
      await goToPatientInfo(tester, "John Smith", true);

      await tester.tap(find.textContaining("2022-02-02")); // click the already existing date with one pre evaluation
      await tester.pumpAndSettle(const Duration(seconds: 3));

      // selecing post-evaluation button
      await tester.tap(find.text("Post Evaluation: Not Started"));
      await tester.pumpAndSettle();

      // accepts the create option from popup
      await tester.tap(find.text("Create"));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key("HeadTab"))); // HEAD TAB
      await tester.pumpAndSettle();

      // HEAD
      await tester.tap(find.byKey(Key(PoseType.HEAD_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      // HEAD ANT
      await tester.tap(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      await tester.tap(find.byIcon(Icons.arrow_back));
      await tester.pumpAndSettle();

      await goToPatientInfo(tester, "John Smith", true); // refresh the page

      await tester.pumpAndSettle(const Duration(seconds: 3));

      await tester.tap(find.textContaining("2022-02-02"));
      await tester.pumpAndSettle(const Duration(seconds: 3));

      expect(find.text("Post Evaluation: In Progress"), findsOneWidget);

      await tester.tap(find.text("Post Evaluation: In Progress"));
      await tester.pumpAndSettle();

      final headWidget =
          tester.widget(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      expect((headWidget as FormBuilderChipOption).value, 0);

      final headAntWidget =
          tester.widget(find.byKey(Key(PoseType.HEAD_ANT_NEUTRAL.toString())));
      expect((headAntWidget as FormBuilderChipOption).value, 0);
    });
  });
}
