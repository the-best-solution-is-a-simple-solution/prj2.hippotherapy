import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:frontend/controllers/local_controllers/local_storage.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:uuid/uuid.dart';

/// A class to load and store local data for evaluations
class LocalEvaluationController {
  static const FlutterSecureStorage storage = FlutterSecureStorage();
  static Uuid uuid = const Uuid();
  static const String COLLECTION_NAME_START = "evaluations_";

  /// Get the key for the collection storing a list of evaluation ids
  /// under the provided patient and session
  static String getEvaluationCollectionKey(final String sessionId) {
    return COLLECTION_NAME_START + sessionId;
  }

  /// Save evaluation under patient provided
  static Future<String> createEvaluation(final PatientEvaluation eval) async {
    // Generate time based uuid
    final String evalId = uuid.v1();
    eval.evaluationID = evalId;
    final jsonData = jsonEncode(eval.toJson());

    try {
      // Save object to localstorage
      await storage.write(key: evalId, value: jsonData);

      // Save reference to it in collection
      final String collection = getEvaluationCollectionKey(eval.sessionID);
      await AppLocalStorage.addToCollection(collection, evalId);
      return evalId;
    }
    // If any error return false
    catch (e) {
      debugPrint("Error: could not locally save eval under session ${eval.sessionID}");
      return "";
    }
  }

  /// Gets evaluation by id
  static Future<PatientEvaluation?> getEvaluationById(final String id) async {
    String? jsonString = "";
    try {
      // read from local storage
      jsonString = await storage.read(key: id);

      // make sure it succeeded
      if (jsonString != null) {
        var json = jsonDecode(jsonString);
        return PatientEvaluation.fromJson(json);
      }
    }
    catch (e) {
      debugPrint("Error in getEvaluationById() Local Eval Controller: could not get evaluation $id... $e");
      debugPrint("\n\nJSON DATA");
      debugPrint(jsonString);
      debugPrint("\n\n");
    }
    return null;
  }

  /// Get all evaluations under the session
  static Future<List<PatientEvaluation>> getEvaluationsForSession(
       final String sessionId) async {
    debugPrint("getting evals for session $sessionId");

    // Get list of eval Ids
    final String collection = getEvaluationCollectionKey(sessionId);
    debugPrint("getting evals form collection $collection");

    final List<String> idList = await AppLocalStorage.getCollection(collection);
    final List<PatientEvaluation> evals = [];
    // Fetch each one
    for (String id in idList) {
      final PatientEvaluation? session = await getEvaluationById(id);
      if (session != null) {
        evals.add(session);
      }
    }
    return evals;
  }





}