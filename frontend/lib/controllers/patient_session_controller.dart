import 'dart:convert';

import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/session.dart';
import 'package:http/http.dart' as http;

class PatientSessionController {
  static final AuthController _authController = AuthController();

  // No need to initialize it because AuthController uses a singleton and every instance is the same
  static final AuthController authController = AuthController();

  /// This will send the passed in session to the db
  /// and return the result of that operation
  static Future<http.Response> createSession(final Session s) async {
    final route = ServerConfig.getCreateSessionRoute(s.patientID);

    final body = jsonEncode(s.toJson());
    final res = await _authController.makeAuthenticatedPostRequest(route, body);

    if (res.statusCode == 200) {
      return res;
    } else {
      throw Exception('Failed to add the session ${res.body} at $route');
    }
  }

  ///Get all sessions for the provided patient
  static Future<List<Session>> getAllSessions(final String? patientID) async {
    if (patientID == null) {
      throw Exception('Patient ID cannot be null');
    }

    final route = ServerConfig.getAllSessionsRoute(patientID);
    final response = await authController.makeAuthenticatedGetRequest(route);

    if (response.statusCode == 200) {
      // If the server did return a 200 OK response, then parse the JSON.
      final List<dynamic> jsonList = jsonDecode(response.body) as List<dynamic>;
      return jsonList.map((final json) => Session.fromJson(json)).toList();
    }

    if (response.statusCode == 204) {
      return [];
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception(
          'Failed to load sessions for $route patient: $patientID status: ${response.statusCode}');
    }
  }

  ///Get pre and post evaluations for provided patient's session
  static Future<List<PatientEvaluation>> getPrePostEvaluations(
      final String patientID, final String sessionID) async {
    final route = ServerConfig.getPrePostEvaluationsRoute(patientID, sessionID);
    final response = await _authController.makeAuthenticatedGetRequest(route);

    if (response.statusCode == 200 || response.statusCode == 206) {
      // If the server did return a 200 OK response, then parse the JSON.
      final List<dynamic> jsonList = jsonDecode(response.body) as List<dynamic>;
      return jsonList
          .map((final json) => PatientEvaluation.fromJson(json))
          .toList();
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      return [];
    }
  }

  /// grabs a list of strings that are unique locations
  static Future<List<String>> fetchSessionUniqueLocations() async {
    final String route = ServerConfig.getUniqueLocationsRoute(_authController.ownerId!);
    final res = await authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      return List<String>.from(jsonList);
    } else {
      throw Exception(res.body);
    }
  }

  /// grabs the list based lowest year and the highest year
  static Future<int> fetchLowestDate() async {
    final String route = ServerConfig.getLowestYearRoute(_authController.ownerId!);
    final res = await authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      return jsonDecode(res.body);
    } else {
      throw Exception(res.body);
    }
  }
}
