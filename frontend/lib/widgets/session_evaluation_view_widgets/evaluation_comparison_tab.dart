import 'dart:async';

import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_evaluation_controller.dart';
import 'package:frontend/controllers/patient_session_controller.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/pose.dart';
import 'package:frontend/widgets/helper_widgets/icon_message_widget.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/evaluation_comparison_row_widget.dart';

/// A page that compares the two evaluations provided in the session
class EvaluationComparisonTab extends StatefulWidget {
  const EvaluationComparisonTab(
      {super.key, required this.sessionID, required this.patientID});

  final String patientID;
  final String sessionID;

  @override
  State<EvaluationComparisonTab> createState() =>
      _EvaluationComparisonTabState();
}

class _EvaluationComparisonTabState extends State<EvaluationComparisonTab> {
  bool isLoading = true; // Track loading state
  bool hasError = false; // Track error state
  List<EvaluationComparisonRow> poseComparisonList = [];

  @override
  void initState() {
    super.initState();
    initList();
  }

  // Initialize the list
  void initList() async {
    await setPoses();
  }

  // Set the patients after init
  Future<void> setPoses() async {
    try {
      List<PatientEvaluation> evals = [];
      final AuthController authController = AuthController();
      if (authController.isGuestLoggedIn()) {
        evals = await LocalEvaluationController.getEvaluationsForSession(widget.sessionID);
      }
      else {
        evals = await PatientSessionController.getPrePostEvaluations(
            widget.patientID, widget.sessionID);
      }
          //
          // await PatientSessionController.getPrePostEvaluations(
          //     widget.patientID, widget.sessionID);

      if (mounted && evals.isEmpty) {
        // No evaluations found
        setState(() {
          hasError = true;
          isLoading = false;
        });
        return;
      }

      if (evals.length == 1) {
        // Indicate only one eval found
        final List<PoseType> poses =
            ConvertEvaluationToPoseType.convertToPoseTypes(evals[0]);
        final String type = evals[0].evalType;
        if (type == 'pre' || type == 'post') {
          for (int i = 0; i < poses.length; i++) {
            final EvaluationComparisonRow row = EvaluationComparisonRow(
                poses[i].category.displayName,
                type == 'pre' ? poses[i] : null,
                type == 'post' ? poses[i] : null);
            poseComparisonList.add(row);
          }
        } else {
          throw Exception(
              'Invalid evaluation type expected pre or post got ${evals[0].evalType}');
        }
      } else if (evals.length == 2) {
        // Convert to pose types (always returned pre, then post)
        final List<PoseType> prePoses =
            ConvertEvaluationToPoseType.convertToPoseTypes(evals[0]);
        final List<PoseType> postPoses =
            ConvertEvaluationToPoseType.convertToPoseTypes(evals[1]);

        // Go through poses and make the rows to display
        for (int i = 0; i < prePoses.length; i++) {
          final EvaluationComparisonRow row = EvaluationComparisonRow(
              prePoses[i].category.displayName, prePoses[i], postPoses[i]);
          poseComparisonList.add(row);
        }
      } else {
        throw Exception(
            'Expected 2 or less evaluations but found ${evals.length}');
      }
    } catch (e) {
      // Handle any errors that occur during the fetching process
      mounted
          ? setState(() {
              hasError = true;
              isLoading = false;
            })
          : null;
      return;
    }

    // Update loading state
    mounted
        ? setState(() {
            isLoading = false;
          })
        : null;
  }

  @override
  Widget build(final BuildContext context) {
    return isLoading
        ? const Center(
            child: CircularProgressIndicator()) // Show loading indicator
        : hasError
            ? const IconMessageWidget(
                message: "No evaluations for session",
              )
            : ListView(
                padding: const EdgeInsets.all(8),
                children: poseComparisonList
                    .map(
                      (final comparison) => SizedBox(
                        height: 100,
                        child: comparison,
                      ),
                    )
                    .toList(),
              );
  }
}
