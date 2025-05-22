
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_storage.dart';
import 'package:frontend/models/patient.dart';
import 'package:get/get_connect/http/src/response/response.dart' as http;
import 'package:uuid/uuid.dart';

/// A class to load and store local data for patients
class LocalPatientController {
  static const FlutterSecureStorage storage = FlutterSecureStorage();
  static Uuid uuid = const Uuid();
  static final AuthController authController = AuthController();
  static const String COLLECTION_NAME = "patients";

  /// Gets all local patients assigned to the provided therapist
  static Future<List<Patient>> getPatientsByTherapistId(final String therapistId) async {
    // Get list of patientIds
    final String collection = getPatientCollectionKey(therapistId);
    final List<String> patientIdList = await AppLocalStorage.getCollection(collection);
    final List<Patient> patients = [];
    // Fetch each one
    for (String id in patientIdList) {
      final Patient? patient = await getPatientByID(id);
      if (patient != null) {
        patients.add(patient);
      }
    }
    return patients;
  }

  /// Saves a patient locally
  /// must have set a therapistId in patient
  /// returns id of patient if successful, empty string otherwise
  static Future<String> savePatient(final Patient patient) async {
    // Generate time based uuid
    final String patientId = uuid.v1();
    patient.id = patientId;

    final jsonData = jsonEncode(patient);

    // return false if no therapistId
    if (patient.therapistId == null) {
      return "";
    }

    try {
      // Save object to localstorage
      await storage.write(key: patientId, value: jsonData);

      // Save reference to it in collection
      final String collection = getPatientCollectionKey(patient.therapistId!);
      await AppLocalStorage.addToCollection(collection, patientId);
      return patientId;
    }
    // If any error return false
    catch (e) {
      debugPrint("Error: could not locally save patient ${patient.id} under"
          "${patient.therapistId}");
      return "";
    }
  }

  /// Updates a patient locally
  /// must have set a therapistId in patient
  /// returns true if success, false otherwise
  static Future<bool> updatePatient(final Patient patient) async {
    // return false if no therapistId or patientId
    if (patient.therapistId == null || patient.id == null) {
      return false;
    }

    final String patientId = patient.id!;
    final jsonData = jsonEncode(patient);

    try {
      // Save object to localstorage
      await storage.write(key: patientId, value: jsonData);

      // Save reference to it in collection
      final String collection = getPatientCollectionKey(patient.therapistId!);
      await AppLocalStorage.addToCollection(collection, patientId);
      return true;
    }
    // If any error return false
    catch (e) {
      debugPrint("Error: could not locally save patient ${patient.id} under"
          "${patient.therapistId}");
      return false;
    }
  }

  /// Gets a locally stored patient by their id
  static Future<Patient?> getPatientByID(final String patientId) async {
    try {
      // read from local storage
      final patientString = await storage.read(key: patientId);

      // make sure it succeeded
      if (patientString != null) {
        var patientJson = jsonDecode(patientString);
        return Patient.fromJson(patientJson);
      }
    }
    catch (e) {
      debugPrint("Error: could not get patient $patientId... $e");
    }
    return null;
  }

  /// Get the key for the collection storing a list of patient ids
  /// under the provided therapist
  static String getPatientCollectionKey(final String therapistId) {
    return COLLECTION_NAME + therapistId;
  }
}