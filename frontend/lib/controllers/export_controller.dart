import 'dart:convert';

import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';

class ExportController {
  /// grabs a list of strings that have unique conditions
  static Future<List<List<String>>> fetchFilteredRecords(
      final String? patientName,
      final String? condition,
      final String? location,
      final String? dateTime) async {
    final queryParameters = <String, String>{};

    if (patientName != null && patientName.isNotEmpty) {
      queryParameters['patientName'] = patientName;
    }
    if (condition != null && condition.isNotEmpty) {
      queryParameters['condition'] = condition;
    }
    if (location != null && location.isNotEmpty) {
      queryParameters['location'] = location;
    }
    if (dateTime != null && dateTime.isNotEmpty) {
      queryParameters['dateTime'] = dateTime;
    }

    final AuthController authController = AuthController();
    final route = ServerConfig.getRecordsRoute(authController.ownerId!);
    final res = await authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      return jsonList
          .map((final dynamic item) => List<String>.from(item))
          .toList();
    } else {
      throw Exception(res.body);
    }
  }
}
