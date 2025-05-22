import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/patient_evaluation_graph_data.dart';
import 'package:http/http.dart' as http;

class PatientEvaluationController {
  static final AuthController authController = AuthController();

  /// Returns a dictionary of the existing cached evaluation data,
  /// sessionID and selected images
  /// or null if it finds nothings, or an exception
  /// if the response was not (OK200/NOTFOUND404)
  static Future<Map<String, dynamic>?> getCachedEval(final String patientID,
      final String sessionID, final String evalType) async {
    final res = await authController.makeAuthenticatedGetRequest(
        ServerConfig.getExistingCachedRoute(patientID, sessionID, evalType));

    if (res.statusCode == HttpStatus.ok) {
      return jsonDecode(res.body);
    } else if (res.statusCode == HttpStatus.notFound) {
      return null;
    } else {
      throw Exception('Status ${res.statusCode}: ${res.body}');
    }
  }

  /// This method takes in a partially completed evaluation form in
  /// a dictionary as K/V pairs, needs a valid sessionID or it will return
  /// a BadRequest (400) error or NotFound (404) error
  /// Still requires posting to a an existing session, and
  static Future<void> cacheEval(final String patientID,
      final Map<String, dynamic> jsonVals, final String sessionID) async {
    final res = await authController.makeAuthenticatedPostRequest(
        ServerConfig.getSaveCacheRoute(patientID, sessionID),
        jsonEncode(jsonVals));

    if (res.statusCode != HttpStatus.ok) {
      throw Exception(res.body);
    }
  }

  /// This method takes in sessionID to delete all cached evaluations for
  static Future<void> clearCachedEval(final String patientID,
      final String sessionID, final String evalType) async {
    final res = await authController.makeAuthenticatedDeleteRequest(
        ServerConfig.getClearSavedCacheRoute(patientID, sessionID, evalType));

    if (res.statusCode != HttpStatus.ok) {
      throw Exception('Failed to delete cached eval ${res.body}');
    }
  }

  static Future<void> postEvaluationToDatabase(
      final PatientEvaluation eval, final String patientID) async {
    final route = ServerConfig.getCreatePatientEvaluationRoute(patientID);
    final body = jsonEncode(eval.toJson());
    final response =
        await authController.makeAuthenticatedPostRequest(route, body);

    if (response.statusCode == 200) {
      debugPrint('Form submitted successfully');
    } else {
      debugPrint(route);
      throw Exception(
          'Failed to submit form: ${response.body} Code: ${response.statusCode}');
    }
  }

  static Future<List<PatientEvaluationGraphData>> getPatientEvaluations(
      final String? patientID) async {
    if (patientID == null) {
      return [];
    }
    return await fetchPatientEvaluationsForOnePatient(patientID);
  }

  static Future<List<PatientEvaluationGraphData>>
      fetchPatientEvaluationsForOnePatient(final String patientID) async {
    final route = ServerConfig.getAllEvaluationDataForGraphRoute(patientID);
    final response = await authController.makeAuthenticatedGetRequest(route);

    if (response.statusCode == 200) {
      // If the server did return a 200 OK response, then parse the JSON.
      final List<dynamic> jsonList = jsonDecode(response.body) as List<dynamic>;
      return jsonList
          .map((final json) => PatientEvaluationGraphData.fromJson(json))
          .toList();
    }
    if (response.statusCode == 204) {
      return [];
    } else {
      debugPrint('\n\nfailed to get data from: $route');
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception(
          'Failed to load evaluations for patient: $patientID status: ${response.statusCode}');
    }
  }

  //region tagging and notes

  static Future<List<String>> getEvaluationTagList() async {
    final route = ServerConfig.getEvaluationTagListRoute();
    final response = await http
        .get(Uri.parse(route), headers: {"Content-Type": "application/json"});
    if (response.statusCode == 200) {
      // return jsonDecode(response.body) as List<String>;
      final List<dynamic> jsonList = jsonDecode(response.body) as List<dynamic>;
      return jsonList.map((final json) => json.toString()).toList();
    }
    if (response.statusCode == 204) {
      return [];
    } else {
      debugPrint('\n\nFailed to get data from: $route');
      throw Exception('Failed to load tag list');
    }
  }

  static Future<List<String>> getEvaluationsTagsFromNotes(
      final String? notes) async {
    final List<String> validTags = await getEvaluationTagList();
    if (validTags.isEmpty || notes == null) {
      return [];
    }
    final List<String> tagList = [];
    for (int i = 0; i < notes.length; i++) {
      if (notes[i] != '#') {
        continue;
      }
      final String restOfNotes = notes.substring(i + 1).trim();

      if (restOfNotes.isEmpty) {
        return tagList;
      }

      final int indexOfSpace = restOfNotes.indexOf(' ');
      final int indexOfHash = restOfNotes.indexOf('#');
      int endOfTagIndex;

      if (indexOfHash == -1 && indexOfSpace == -1) {
        if (validTags.contains(restOfNotes) && !tagList.contains(restOfNotes)) {
          tagList.add(restOfNotes);
        }
        return tagList;
      }

      if (indexOfHash == -1) {
        endOfTagIndex = indexOfSpace;
      } else if (indexOfSpace == -1) {
        endOfTagIndex = indexOfHash;
      } else if (indexOfHash < indexOfSpace) {
        endOfTagIndex = indexOfHash;
      } else {
        endOfTagIndex = indexOfSpace;
      }

      if (endOfTagIndex <= 0) {
        // Empty tag
        i++;
        continue;
      }

      final String tag = restOfNotes.substring(0, endOfTagIndex).trim();
      if (validTags.contains(tag) && !tagList.contains(tag)) {
        tagList.add(tag);
      }

      // Tag is in the middle or beginning of the string, doesn't include the space or the #
      i += endOfTagIndex;
    }
    return tagList;
  }

  //endregion
}
