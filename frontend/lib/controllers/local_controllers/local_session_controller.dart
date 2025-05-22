import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:frontend/controllers/local_controllers/local_storage.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/session.dart';
import 'package:uuid/uuid.dart';

/// A class to load and store local data for sessions
class LocalSessionController {
  static const FlutterSecureStorage storage = FlutterSecureStorage();
  static Uuid uuid = const Uuid();
  static const String COLLECTION_NAME_START = "sessions_";

  static Future<List<PatientEvaluation>> getPrePostEvaluations(
      final String patientID, final String sessionID) async {
    throw UnimplementedError();
  }

  /// Gets all sessions for the provided patientId
  static Future<List<Session>> getAllSessions(final String? patientId) async {
    // Return empty if no patientId provided
    if (patientId == null) {
      debugPrint("Error no patientId provided for getting all sessions");
      return [];
    }
    
    // Get list of sessionIds
    final String collection = getPatientSessionCollectionKey(patientId);
    final List<String> idList = await AppLocalStorage.getCollection(collection);
    final List<Session> sessions = [];
    // Fetch each one
    for (String id in idList) {
      final Session? session = await getSessionById(id);
      if (session != null) {
        sessions.add(session);
      }
    }
    return sessions;
  }

  /// Creates a session
  static Future<bool> createSession(final Session session) async {
    // Generate time based uuid
    final String sessionId = uuid.v1();
    session.sessionID = sessionId;
    final jsonData = jsonEncode(session);
    
    try {
      // Save object to localstorage
      await storage.write(key: sessionId, value: jsonData);

      // Save reference to it in collection
      final String collection = getPatientSessionCollectionKey(session.patientID);
      await AppLocalStorage.addToCollection(collection, sessionId);
      return true;
    }
    // If any error return false
    catch (e) {
      debugPrint("Error: could not locally save session under ${session.patientID}");
      return false;
    }
  }


  /// Gets session by id
  static Future<Session?> getSessionById(final String id) async {
    try {
      // read from local storage
      final jsonString = await storage.read(key: id);

      // make sure it succeeded
      if (jsonString != null) {
        var json = jsonDecode(jsonString);
        return Session.fromJson(json);
      }
    }
    catch (e) {
      debugPrint("Error: could not get session $id... $e");
    }
    return null;
  }


  /// Get the key for the collection storing a list of patient ids
  /// under the provided patient
  static String getPatientSessionCollectionKey(final String patientId) {
    return COLLECTION_NAME_START + patientId;
  }

}