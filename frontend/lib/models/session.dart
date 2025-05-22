class Session {
  String patientID;
  String? sessionID;
  String location;
  DateTime dateTaken;

  Session({
    required this.patientID,
    this.sessionID,
    required this.location,
    required this.dateTaken,
  });

  Session.fromJson(final Map<String, dynamic> json)
      : patientID = json['patientID'],
        sessionID = json['sessionID'] ?? "",
        dateTaken = DateTime.parse(json['dateTaken']),
        // Parse the datetime string
        location = json['location'];

  Map<String, dynamic> toJson() => {
        'sessionID': sessionID ?? "",
        'patientID': patientID,
        'location': location,
        // Convert DateTime to string when sending
        'dateTaken': dateTaken.toIso8601String()
      };

  factory Session.placeholderSession() {
    return Session(
        patientID: '123',
      sessionID: '123',
      location: 'Saskatoon',
      dateTaken: DateTime.now(),
    );
  }
}
