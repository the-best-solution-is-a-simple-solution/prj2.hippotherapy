class PatientEvaluationGraphData {
  final DateTime dateTaken;
  final String sessionID;
  final String evaluationID;
  final String evalType;
  final String? notes;
  final bool exclude;
  final int hipFlex;
  final int lumbar;
  final int headAnt;
  final int headLat;
  final int kneeFlex;
  final int pelvic;
  final int pelvicTilt;
  final int thoracic;
  final int trunk;
  final int trunkInclincation;
  final int elbowExtension;
  double calculatedPositiveAverage = 0;
  double calculatedNegativeAverage = 0;

  PatientEvaluationGraphData({
    required this.dateTaken,
    required this.sessionID,
    required this.evaluationID,
    this.notes,
    required this.exclude,
    required this.evalType,
    required this.hipFlex,
    required this.lumbar,
    required this.headAnt,
    required this.headLat,
    required this.kneeFlex,
    required this.pelvic,
    required this.pelvicTilt,
    required this.thoracic,
    required this.trunk,
    required this.trunkInclincation,
    required this.elbowExtension,
  }) {
    _calculateAverages();
  }

  void _calculateAverages() {
    // calculate average
    final List<int> postureValues = [
      hipFlex,
      lumbar,
      headAnt,
      headLat,
      kneeFlex,
      pelvic,
      pelvicTilt,
      thoracic,
      trunk,
      trunkInclincation,
      elbowExtension
    ];

    final List<int> positiveValues =
        postureValues.where((final v) => v > 0).toList();
    final List<int> negativeValues =
        postureValues.where((final v) => v < 0).toList();

    if (positiveValues.isNotEmpty) {
      calculatedPositiveAverage =
          positiveValues.reduce((final a, final b) => a + b) /
              positiveValues.length;
    }

    if (negativeValues.isNotEmpty) {
      calculatedNegativeAverage =
          negativeValues.reduce((final a, final b) => a + b) /
              negativeValues.length;
    }
  }

  factory PatientEvaluationGraphData.fromJson(final Map<String, dynamic> json) {
    return PatientEvaluationGraphData(
      dateTaken: DateTime.parse(json['dateTaken']),
      sessionID: json['sessionID'] as String,
      evaluationID: json['evaluationID'] as String,
      evalType: json['evalType'] as String,
      notes: json['notes'] as String?,
      exclude: json['exclude'] as bool,
      hipFlex: json['hipFlex'] as int,
      lumbar: json['lumbar'] as int,
      headAnt: json['headAnt'] as int,
      headLat: json['headLat'] as int,
      kneeFlex: json['kneeFlex'] as int,
      pelvic: json['pelvic'] as int,
      pelvicTilt: json['pelvicTilt'] as int,
      thoracic: json['thoracic'] as int,
      trunk: json['trunk'] as int,
      trunkInclincation: json['trunkInclination'] as int,
      elbowExtension: json['elbowExtension'] as int,
    );
  }
}
