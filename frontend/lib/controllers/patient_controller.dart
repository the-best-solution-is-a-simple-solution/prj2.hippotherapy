import 'dart:convert';

import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:http/http.dart' as http;

/// Created by: Noah Stewart-Worobec & Aron Szabo
/// Purpose: Handle API calls for Patients
class PatientController {
// sets sane defaults before env load.
  static final AuthController _authController = AuthController();

  static Future<List<Patient>> getPatientsByTherapistId(
      String therapistId) async {
    List<Patient> patients = [];
    final route = ServerConfig.getPatientListByTherapistIdRoute(therapistId);

    final res = await _authController.makeAuthenticatedGetRequest(route);
    if (res.statusCode == 200) {
      // If the server did return a 200 OK response, then parse the JSON.
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      // debugPrint('Loaded ${jsonList.length} patients');
      patients = jsonList.map((final json) => Patient.fromJson(json)).toList();
      return patients;
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception('Failed to load patients');
    }
  }

  /// This method will take in a patient, convert it to JSON and post it to the
  /// backend, returning its ID from the backend upon successful insertion
  /// or throws an error
  static Future<http.Response> postPatientToDatabase(
      final Patient patient) async {
    final id = patient.therapistId;
    final route = ServerConfig.getCreatePatientRoute(id!);
    final body = jsonEncode(patient.toJson());
    final res = await _authController.makeAuthenticatedPostRequest(route, body);

    if (res.statusCode == 200) {
      return res;
    } else {
      throw Exception('Failed to add the patient ${res.body}');
    }
  }

  /// This method will grab a single patient from the backend, taking in a string ID
  /// returning the patient upon success or a 404 not found exception
  static Future<Patient> getPatientByID(final String patientId) async {
    final route = ServerConfig.getPatientByIdRoute(patientId);
    final res = await _authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      return Patient.fromJson(jsonDecode(res.body));
    } else {
      throw Exception(res.body);
    }
  }

  /// This method will take in a patient that will be PUT to
  /// the backend for modification, will throw exception if patient does
  /// not have an ID
  static Future<http.Response> modifyPatientByID(final Patient patient) async {
    if (patient.id == null) {
      throw ArgumentError("Patient must have existing ID");
    }

    final route = ServerConfig.getUpdatePatientRoute(patient.id!);
    final body = jsonEncode(patient.toJson());
    final res = await _authController.makeAuthenticatedPutRequest(route, body);

    if (res.statusCode == 200) {
      return res;
    } else {
      throw Exception('Failed to add the patient ${res.body}');
    }
  }

  /// This method will send a DELETE to the db on the given patientID which
  /// will archive the patient by setting its archival date to now.
  /// Will return a 404 if the patient was not found
  static Future<http.Response> archivePatientByID(final String id) async {
    final route = ServerConfig.getArchivePatientRoute(id);
    final res = await _authController.makeAuthenticatedDeleteRequest(route);

    if (res.statusCode == 200) {
      return res;
    } else {
      throw Exception(res.body);
    }
  }

  /// grabs a list of strings that have unique names
  static Future<List<String>> getUniqueNamesOfPatients(
      final String ownerId) async {
    final String route = ServerConfig.getUniqueNamesRoute(ownerId);
    final res = await _authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      return List<String>.from(jsonList);
    } else {
      throw Exception(res.body);
    }
  }

  /// grabs a list of strings that have unique conditions
  static Future<List<String>> getUniqueConditionsOfPatients(
      final String ownerId) async {
    final String route = ServerConfig.getUniqueConditionsRoute(ownerId);
    final res = await _authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      return List<String>.from(jsonList);
    } else {
      throw Exception(res.body);
    }
  }
}
