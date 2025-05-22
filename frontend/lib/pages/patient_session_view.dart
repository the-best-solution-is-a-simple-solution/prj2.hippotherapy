import 'package:dynamic_tabbar/dynamic_tabbar.dart';
import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_evaluation_controller.dart';
import 'package:frontend/controllers/patient_session_controller.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/session.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/evaluation_comparison_tab.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/evaluation_creation_tab_widget.dart';

// represents the page the view from clicking upon a singular patient session
class PatientSessionView extends StatefulWidget {
  const PatientSessionView(
      {required this.title,
      required this.patient,
      required this.session,
      super.key});

  final String title;
  final Session session;
  final Patient patient;

  @override
  State<StatefulWidget> createState() => _PatientSessionViewState();
}

class _PatientSessionViewState extends State<PatientSessionView> {
  List<PatientEvaluation> evals = [];
  bool isLoading = true;

  // initialize evaluations
  void initEvals() async {
    final AuthController authController = AuthController();
    if (authController.isGuestLoggedIn()) {
      evals = await LocalEvaluationController.getEvaluationsForSession(widget.session.sessionID!);
    }
    else {
      evals = await PatientSessionController.getPrePostEvaluations(
          widget.patient.id!, widget.session.sessionID!);
    }

    if (mounted) {
      setState(() {isLoading = false;});
    }

    // evals = await PatientSessionController.getPrePostEvaluations(
    //     widget.patient.id!, widget.session.sessionID!);
    // setState(() {
    //   isLoading = false;
    // });
  }

  // initialize the title
  void initTitle() async {
    await generateTitle();
  }

  // generate the session title
  Future<void> generateTitle() async {
    AuthController authController = AuthController();
    if (authController.isGuestLoggedIn()) {
      evals = await LocalEvaluationController.getEvaluationsForSession(widget.session.sessionID!);
    }
    else {
      evals = await PatientSessionController.getPrePostEvaluations(
          widget.patient.id!, widget.session.sessionID!);
    }

    if (mounted) {
      setState(() {});
    }
  }

  // initialize the state at first widget load
  @override
  void initState() {
    super.initState();
    initTitle();
    initEvals();
  }

  @override
  Widget build(final BuildContext context) {
    if (isLoading) {
      return Scaffold(
        appBar: AppBar(
          backgroundColor: Theme.of(context).colorScheme.primary,
          title: Text(widget.title),
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: Text(widget.title),
      ),
      body: Center(
        child: DynamicTabBarWidget(
          onAddTabMoveTo: MoveToTab.first,
          dynamicTabs: [
            TabData(
              index: 1,
              title: const Tab(
                child: Text('Evaluations'),
              ),
              content: EvaluationCreationTab(
                patient: widget.patient,
                sessionID: widget.session.sessionID!,
              ),
            ),
            if (evals.length == 2)
              TabData(
                index: 2,
                title: const Tab(
                  key: Key("ComparisonsTab"),
                  child: Text('Comparisons'),
                ),
                content: EvaluationComparisonTab(
                  sessionID: widget.session.sessionID!,
                  patientID: widget.patient.id!,
                ),
              )
          ],
          onTabControllerUpdated: (final controller) {
            controller.index = 0;
          },
          // required to prevent errors
          onTabChanged: (final index) {},
        ),
      ),
    );
  }
}
