import 'dart:convert';

import 'package:frontend/config.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/owner.dart';
import 'package:frontend/models/therapist.dart';
import 'package:http/http.dart' as http;

class OwnerController {
// sets sane defaults before env load.
  static const String _address = ServerConfig.address;
  static const String _port = ServerConfig.port;
  static const String _route = ServerConfig.ownerRoute;
  static final AuthController _authController = AuthController();

  // TODO: remove? not used?
  /// Returns a list of every patient in the database
  static Future<List<Owner>> getOwners() async {
    final res = await http.get(Uri.parse('$_address:$_port$_route'), headers: {
      "Content-Type": "application/json",
    });
    if (res.statusCode == 200) {
      // If the server did return a 200 OK response, then parse the JSON.
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      return jsonList.map((final json) => Owner.fromJson(json)).toList();
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception('Failed to load owners');
    }
  }

  static Future<Owner?> getOwner(final String ownerId) async {
    final route = ServerConfig.getOwnerByIdRoute(ownerId);
    final res = await _authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      // If the server did return a 200 OK response, then parse the JSON.
      final dynamic json = jsonDecode(res.body) as dynamic;
      return json.map((final json) => Owner.fromJson(json));
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception('Failed to load owner');
    }
  }

  static Future<List<Therapist>> getTherapistsByOwnerId(
      final String ownerId) async {
    final route = ServerConfig.getTherapistsByOwnerIdRoute(ownerId);
    final res = await _authController.makeAuthenticatedGetRequest(route);

    if (res.statusCode == 200) {
      // If the server did return a 200 OK response, then parse the JSON.
      final List<dynamic> jsonList = jsonDecode(res.body) as List<dynamic>;
      return jsonList.map((final json) => Therapist.fromJson(json)).toList();
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception('Failed to load therapists');
    }
  }

  static Future<http.Response> reassignPatientsToDiffTherapist(
      final String ownerID,
      final String oldT,
      final String newT,
      final List<String> lstPatientIDs) async {
    final route = ServerConfig.getReassignPatientsToDifferentTherapistRoute(
        ownerID, oldT, newT);
    final body = jsonEncode(lstPatientIDs);
    final res = await _authController.makeAuthenticatedPutRequest(route, body);

    if (res.statusCode == 200) {
      return res;
    } else {
      // If the server did not return a 200 OK response, then throw an exception.
      throw Exception(
          'Failed to reassign patients ${res.statusCode} ${res.toString()}');
    }
  }
}
