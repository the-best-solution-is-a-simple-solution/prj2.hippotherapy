import 'package:flutter/material.dart';
import 'package:frontend/models/pose.dart';

/// A row with a title, and two images retrieved from the poses, an icon for either '=' or '-->'
/// is displayed between them if they are the same (=), otherwise the left arrow.
class EvaluationComparisonRow extends StatelessWidget {
  final String _rowTitle;
  final PoseType? _pose1;
  final PoseType? _pose2;
  final String _iconEqualsPath = "../assets/icons/arrow_right.png";
  final String _iconLeftArrowPath = "../assets/icons/equals_sign.png";
  final String _lineImagePath = "../assets/icons/line.png";
  final double IMAGE_SIZE = 50.0;
  final String keyText = "EvaluationComparisonRow";
  // final List<String> preTagList;
  // final List<String> postTagList;

  // Getters for testing
  String get rowTitle => _rowTitle;

  PoseType? get pose1 => _pose1;

  PoseType? get pose2 => _pose2;

  const EvaluationComparisonRow(this._rowTitle, this._pose1, this._pose2,
      /*this.preTagList, this.postTagList,*/
      {super.key});

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      key: Key(keyText),
      body: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Title row
          Padding(
            padding: const EdgeInsets.only(bottom: 4.0),
            child: Text(
              _rowTitle,
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          // Image comparison row
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: <Widget>[
              Expanded(
                // Image from pre-evaluation
                child: Image.asset(
                  _pose1 != null ? _pose1.imgPath : _lineImagePath,
                  width: _pose1 != null ? IMAGE_SIZE : 0,
                  height: _pose1 != null ? IMAGE_SIZE : 0,
                ),
              ),
              /*Padding(
                  padding:
                      const EdgeInsets.symmetric(vertical: 5, horizontal: 5),
                  child: preTagList.isNotEmpty
                      ? Text(
                          "Tags excluding this evaluation from graph analysis: ${preTagList.toString()}",
                          style: const TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                              color: Colors.red),
                        )
                      : null),*/
              Expanded(
                // Ternary to display '=' icon if same or '-->' icon if different
                child: _pose2 == null || _pose1 == null
                    ? Image.asset(
                        _lineImagePath,
                        width: IMAGE_SIZE,
                        height: IMAGE_SIZE,
                      )
                    : _pose1 == _pose2
                        ? Image.asset(
                            _iconLeftArrowPath,
                            width: IMAGE_SIZE,
                            height: IMAGE_SIZE,
                          )
                        : Image.asset(
                            _iconEqualsPath,
                            width: IMAGE_SIZE,
                            height: IMAGE_SIZE,
                          ),
              ),
              Expanded(
                // Image from post-evaluation or default image if _pose2 is null
                child: Image.asset(
                  _pose2?.imgPath ?? _lineImagePath,
                  // Use default image if _pose2 is null
                  width: _pose2 != null ? IMAGE_SIZE : 0,
                  height: _pose2 != null ? IMAGE_SIZE : 0,
                ),
              ),
              /*Padding(
                  padding:
                      const EdgeInsets.symmetric(vertical: 5, horizontal: 5),
                  child: postTagList.isNotEmpty
                      ? Text(
                          "Tags excluding this evaluation from graph analysis: ${postTagList.toString()}",
                          style: const TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.bold,
                              color: Colors.red),
                        )
                      : null)*/
            ],
          ),
        ],
      ),
    );
  }
}
