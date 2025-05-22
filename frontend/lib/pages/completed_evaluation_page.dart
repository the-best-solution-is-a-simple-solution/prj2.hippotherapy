import 'package:flutter/material.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/pose.dart';

class CompletedEvaluationView extends StatefulWidget {
  const CompletedEvaluationView(
      {required this.eval, required this.title, super.key});

  final PatientEvaluation eval;
  final String title;

  @override
  State<CompletedEvaluationView> createState() =>
      _CompletedEvaluationViewState();
}

String convertToPoseTypeString(final String key) {
  // Handle specific case for headlat
  if (key.toLowerCase() == 'headlat') {
    return 'head';
  }

  final int indexOfFirstCap = key.indexOf(RegExp('[A-Z]'));
  if (indexOfFirstCap != -1) {
    return key.substring(0, indexOfFirstCap) +
        key.replaceRange(0, indexOfFirstCap, "_");
  }
  return key;
}

class _CompletedEvaluationViewState extends State<CompletedEvaluationView> {
  List<Widget> assembleView() {
    final List<Widget> evalData = [];
    debugPrint(widget.eval.toJson().toString());
    widget.eval.toJson().forEach((final k, final v) {
      final String completedString = convertToPoseTypeString(k);

      for (final p in PoseType.values) {
        if (p.category.toString() ==
            "PoseGroup.${completedString.toUpperCase()}") {
          if (p.value == v) {
            evalData.add(Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Image.asset(p.imgPath),
                const SizedBox(height: 2.0),
                Text(
                  p.category.displayName,
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontSize: 20),
                ),
              ],
            ));
            break;
          }
        }
      }
    });

    return evalData;
  }

  @override
  Widget build(final BuildContext context) {
    final List<Widget> evalImages = assembleView();

    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: Text(widget.title),
      ),
      body: Padding(
        padding: const EdgeInsets.all(2.0),
        child: GridView.builder(
          itemCount: 11,
          gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
            crossAxisCount: 2,
            crossAxisSpacing: 2.0,
            mainAxisSpacing: 2.0,
            childAspectRatio: 0.9,
          ),
          itemBuilder: (final context, final index) {
            return evalImages[index];
          },
        ),
      ),
    );
  }
}
