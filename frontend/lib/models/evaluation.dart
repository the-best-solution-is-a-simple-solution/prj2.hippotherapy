class PatientEvaluation {
  String evalType;
  String sessionID;
  String evaluationID;
  bool exclude;
  String? notes;
  int hipFlex;
  int lumbar;
  int headAnt;
  int headLat;
  int kneeFlex;
  int pelvic;
  int pelvicTilt;
  int thoracic;
  int trunk;
  int trunkInclincation;
  int elbowExtension;

  PatientEvaluation(
    this.evalType,
    this.sessionID,
    this.evaluationID,
    this.exclude,
    this.notes,
    this.hipFlex,
    this.lumbar,
    this.headAnt,
    this.headLat,
    this.kneeFlex,
    this.pelvic,
    this.pelvicTilt,
    this.thoracic,
    this.trunk,
    this.trunkInclincation,
    this.elbowExtension,
  );

  factory PatientEvaluation.fromJson(final Map<String, dynamic> json) {
    return switch (json) {
      {
        'sessionID': final String sessionID,
        'evaluationID': final String evaluationID,
        'evalType': final String evalType,
        'exclude': final bool exclude,
        'notes': final String? notes,
        'hipFlex': final int hipFlex,
        'lumbar': final int lumbar,
        'headAnt': final int headAnt,
        'headLat': final int headLat,
        'kneeFlex': final int kneeFlex,
        'pelvic': final int pelvic,
        'pelvicTilt': final int pelvicTilt,
        'thoracic': final int thoracic,
        'trunk': final int trunk,
        'trunkInclination': final int trunkInclincation,
        'elbowExtension': final int elbowExtension,
      } =>
        PatientEvaluation(
            evalType,
            sessionID,
            evaluationID,
            exclude,
            notes,
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
            elbowExtension),
      _ => throw const FormatException('Failed to load Item.'),
    };
  }

  Map<String, dynamic> toJson() {
    return {
      'evalType': evalType,
      'sessionID': sessionID,
      'evaluationID': evaluationID,
      'exclude': exclude,
      'notes': notes,
      'hipFlex': hipFlex,
      'lumbar': lumbar,
      'headAnt': headAnt,
      'headLat': headLat,
      'kneeFlex': kneeFlex,
      'pelvic': pelvic,
      'pelvicTilt': pelvicTilt,
      'thoracic': thoracic,
      'trunk': trunk,
      'trunkInclination': trunkInclincation,
      'elbowExtension': elbowExtension,
    };
  }

  // Factory method for a placeholder evaluation
  factory PatientEvaluation.placeholderEval() {
    return PatientEvaluation(
      'mock_type',        // evalType
      'mock_session_id',  // sessionID
      'mock_eval_id',     // evaluationID
      false,
      '',
      5,                  // hipFlex
      5,                  // lumbar
      5,                  // headAnt
      5,                  // headLat
      5,                  // kneeFlex
      5,                  // pelvic
      5,                  // pelvicTilt
      5,                  // thoracic
      5,                  // trunk
      5,                  // trunkInclination
      5,                  // elbowExtension
    );
  }
}
