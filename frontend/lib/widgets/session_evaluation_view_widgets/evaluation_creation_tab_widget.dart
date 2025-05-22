import 'package:flutter/material.dart';
import 'package:frontend/controllers/patient_evaluation_controller.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_evaluation_controller.dart';
import 'package:frontend/controllers/patient_session_controller.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/pages/completed_evaluation_page.dart';
import 'package:frontend/pages/evaluation_form_page.dart';
import 'package:localstore/localstore.dart';

// the evaluation tab showing the two buttons that navigate the entering input for the evaluation form
class EvaluationCreationTab extends StatefulWidget {
  const EvaluationCreationTab(
      {required this.patient, required this.sessionID, super.key});

  final Patient patient;
  final String sessionID;

  @override
  State<EvaluationCreationTab> createState() => _EvaluationCreationTabState();
}

class _EvaluationCreationTabState extends State<EvaluationCreationTab> {
  List<PatientEvaluation> evals = [];
  String textPre = "Pre Evaluation: ";
  String textPost = "Post Evaluation: ";
  final _db = Localstore.instance;
  List<Widget> buttons = [];
  bool isLoading = true;
  bool firstEvaluation = false;

  @override
  void initState() {
    super.initState();
    initAll();
  }

  Future<void> initAll() async {
    setState(() {
      isLoading = true;
    });
    // If in guest mode get from local data
    final AuthController authController = AuthController();
    if (authController.isGuestLoggedIn()) {
      evals = await LocalEvaluationController.getEvaluationsForSession(widget.sessionID);
    }
    else {
      evals = await PatientSessionController.getPrePostEvaluations(
          widget.patient.id.toString(), widget.sessionID);
    }

    if (mounted) {
      final newButtons = await generateButtons();
      setState(() {
        buttons = newButtons;
        isLoading = false;
      });
    }
  }

  // generate the buttons based on evaluation state
  Future<List<Widget>> generateButtons() async {
    textPre = "Pre Evaluation: ";
    textPost = "Post Evaluation: ";

    // Check online for data first, if found use that, otherwise fallback to localstorage
    final AuthController _authController = AuthController();
    var existingPreDataInCloudData;
    var existingPostDataInCloudData;
    if (!_authController.isGuestLoggedIn()) {
      existingPreDataInCloudData =
      await PatientEvaluationController.getCachedEval(
          widget.patient.id!, widget.sessionID, "pre");
      existingPostDataInCloudData =
      await PatientEvaluationController.getCachedEval(
          widget.patient.id!, widget.sessionID, "post");
    }
    

    final bool existingPreInCloud = existingPreDataInCloudData != null;
    final bool existingPostInCloud = existingPostDataInCloudData != null;

    // check local storage for saved form data
    final preData = await _db
        .collection('evaluations')
        .doc('${widget.sessionID}_pre')
        .get();

    final postData = await _db
        .collection('evaluations')
        .doc('${widget.sessionID}_post')
        .get();

    // if neither exists in the cloud, fallback to the original method
    // else we can set it based on the status
    switch (evals.length) {
      case 0:
        {
          // ff no completed evals, check local storage,
          if (existingPreInCloud) {
            textPre += "In Progress";
          } else {
            textPre += preData != null ? "In Progress" : "Not Started";

            textPost += postData != null ? "In Progress" : "Not Started";
          }

          firstEvaluation = true;
        }
        break;
      case 1:
        {
          // one completed eval exists
          textPre += "Completed";
          if (existingPostInCloud) {
            textPost += "In Progress";
          } else {
            textPost += postData != null ? "In Progress" : "Not Started";
          }
        }
        break;
      case 2:
        {
          textPre += "Completed";
          textPost += "Completed";
        }
        break;
      default:
        {
          textPre += "Error";
          textPost += "Error";
        }
    }

    return [
      ElevatedButton(
          onPressed: () {
            if (textPre.split(" ").contains('Completed')) {
              pushToFormOrCompletedEval(CompletedEvaluationView(
                key: widget.key,
                eval: evals.last,
                title:
                    '${widget.patient.fName} ${widget.patient.lName}\'s Pre-Assessment Evaluation',
              ));
            } else if (textPre.split(" ").contains('Progress')) {
              pushToFormOrCompletedEval(EvaluationForm(
                sessionID: widget.sessionID,
                evalType: "pre",
                patient: widget.patient,
                data: existingPreDataInCloudData ?? preData,
              ));
            } else {
              sessionButtonViewOrCreateEval("Pre-Evaluation");
            }
          },
          child: Text(textPre)),
      const SizedBox(
        height: 100,
      ),
      if (!firstEvaluation)
        ElevatedButton(
          onPressed: () {
            if (textPost.split(" ").contains('Completed')) {
              pushToFormOrCompletedEval(CompletedEvaluationView(
                key: widget.key,
                eval: evals.last,
                title:
                    '${widget.patient.fName} ${widget.patient.lName}\'s Post-Assessment Evaluation',
              ));
            } else if (textPost.split(" ").contains('Progress')) {
              pushToFormOrCompletedEval(EvaluationForm(
                sessionID: widget.sessionID,
                evalType: "post",
                patient: widget.patient,
                data: existingPostDataInCloudData ?? postData,
              ));
            } else {
              sessionButtonViewOrCreateEval("Post-Evaluation");
            }
          },
          child: Text(textPost),
        ),
    ];
  }

  void pushToFormOrCompletedEval(final Widget w) {
    Navigator.push(context, MaterialPageRoute(builder: (final context) => w));
  }

  Future<void> sessionButtonViewOrCreateEval(final String evalType) async {
    final String evalTypeSmall = evalType.split('-')[0].toLowerCase();
    return showDialog<void>(
      context: context,
      barrierDismissible: false, // user must tap button!
      builder: (final BuildContext context) {
        return AlertDialog(
          content: SingleChildScrollView(
            child: ListBody(
              children: <Widget>[
                Text('Create $evalType'),
              ],
            ),
          ),
          actions: <Widget>[
            TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                },
                child: const Text("Cancel")),
            TextButton(
              child: const Text('Create'),
              onPressed: () {
                Navigator.of(context).pop();
                pushToFormOrCompletedEval(EvaluationForm(
                  sessionID: widget.sessionID,
                  evalType: evalTypeSmall,
                  patient: widget.patient,
                  data: null,
                ));
              },
            ),
          ],
        );
      },
    );
  }

  @override
  Widget build(final Object context) {
    return Center(
      child: isLoading
          ? const CircularProgressIndicator()
          : Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: buttons,
            ),
    );
  }
}
