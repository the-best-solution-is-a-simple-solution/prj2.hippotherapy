import 'dart:convert';

import 'package:flutter/cupertino.dart';
import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:http/http.dart' as http;

class ArchiveController {
  static final AuthController _authController = AuthController();

  Future<http.Response> restorePatient(final String patientId) async {
    final route = ServerConfig.getRestorePatientRoute(patientId);
    final response =
        await _authController.makeAuthenticatedPutRequest(route, null);
    return response;
  }

  Future<http.Response> deletePatient(final String patientId) async {
    final route = ServerConfig.getDeletePatientRoute(patientId);
    final response =
        await _authController.makeAuthenticatedDeleteRequest(route);
    return response;
  }

  /// Returns a list of archived patients for a given therapist
  Future<List<Patient>> getArchivedPatientList(final String therapistId) async {
    try {
      final route =
          ServerConfig.getArchivedPatientListByTherapistIdRoute(therapistId);
      final res = await _authController.makeAuthenticatedGetRequest(route);
      if (res.statusCode == 200) {
        final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
        return jsonList.map((final json) => Patient.fromJson(json)).toList();
      } else {
        throw Exception(
            'Failed to load archived patients: ${res.statusCode} - ${res.body}');
      }
    } catch (e) {
      debugPrint('Error fetching archived patients: $e');
      rethrow;
    }
  }
}
