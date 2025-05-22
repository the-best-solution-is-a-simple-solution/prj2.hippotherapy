import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/patient_session_controller.dart';
import 'package:http/http.dart' as http;

void main() {
  const String patientWithSessions =
      "patient-with-sessions-0cc9-4539-9b1e-1db2c1163fe1";

  String address;
  String port;
  String testsController;

  group('test the session controller', () {
    setUpAll(() async {
      // seed data
      address = ServerConfig.address;
      port = ServerConfig.port;
      testsController = ServerConfig.integrationTestsRoute;

      String url = '$address:$port/$testsController/clear';
      var response = await http.delete(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);

      url = '$address:$port/$testsController/seed-patient-info-session-tab';
      response = await http.post(Uri.parse(url));
      debugPrint('Status: ${response.statusCode} for $url');
      expect(response.statusCode, 200);
    });

    test('Tests getAllSessions throws error if empty', () async {
      expect(PatientSessionController.getAllSessions('patient-no-data'),
          throwsException);
    });

    test(
        'Tests that getAllSessions returns list of patient sessions if data present',
        () async {
      final sessions =
          await PatientSessionController.getAllSessions(patientWithSessions);
      expect(sessions.isNotEmpty, true);
      expect(sessions[0].patientID == patientWithSessions, true);
    });
  });
  test(
      'Tests that getPrePostEvaluations returns list of PatientEvaluations with pre and post evaluations',
      () async {
    const String patientID = patientWithSessions;
    final sessions = await PatientSessionController.getAllSessions(patientID);
    expect(sessions.isNotEmpty, true);
    final evaluations = await PatientSessionController.getPrePostEvaluations(
        patientID, sessions[0].sessionID!);
    expect(evaluations.isNotEmpty, true);
    // should have pre and post evaluation
    expect(evaluations[0].evalType == 'pre', true);
    expect(evaluations[1].evalType == 'post', true);
  });

  test('Tests that getPrePostEvaluations returns list of PatientEvaluations',
      () async {
    const String patientID = patientWithSessions;
    final sessions = await PatientSessionController.getAllSessions(patientID);
    expect(sessions.isNotEmpty, true);
    final evaluations = await PatientSessionController.getPrePostEvaluations(
        patientID, sessions[0].sessionID!);
    expect(2, evaluations.length);
    // should have pre evaluation
    expect(evaluations[0].evalType == 'pre', true);
  });

  test('Test that expects 2022 to be the lowest date across all sessions',
      () async {
    final lowestDate = await PatientSessionController.fetchLowestDate();
    // Checks that the lowest year is 2022
    expect(lowestDate, 2022);
  });

  test('Test that fetches unique locations', () async {
    final uniqueLocations =
        await PatientSessionController.fetchSessionUniqueLocations();
    // Checks that the unique locations list is not empty
    expect(uniqueLocations.isNotEmpty, true);
    // Checks that the unique locations list contains specific locations
    expect(uniqueLocations, contains('NA'));
    expect(uniqueLocations, contains('CA'));
    expect(uniqueLocations, contains('LA'));
    expect(uniqueLocations, contains('PA'));
    expect(uniqueLocations.length, 4);
  });
}
