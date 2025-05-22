import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:frontend/config.dart';
import 'package:frontend/models/owner.dart';
import 'package:frontend/models/role.dart';
import 'package:frontend/models/therapist.dart';
import 'package:get/get_connect/http/src/status/http_status.dart';
import 'package:http/http.dart' as http;

/// This class handles authentication and registration for therapists.
class AuthController extends ChangeNotifier {
  // ----------Setup for making it a singleton--------//
  AuthController._();

  // Single instance
  static final AuthController _instance = AuthController._();

  // Factory constructor that returns the singleton instance
  factory AuthController() {
    return _instance;
  }

  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();
  String? _token;
  String? _therapistId;
  String? _ownerId;
  String? _guestId;
  bool _isOwnerLoggedIn = false;
  bool _isTherapistLoggedIn = false;
  bool _isGuestLoggedIn = false;

  /// Returns true if user is logged in as a guest
  bool isGuestLoggedIn() {
    return _guestId != null;
  }

  /// This method will take in an email for an existing firebase
  /// auth user, and if valid will send the user an email
  /// otherwise it will raise an exception which the calling
  /// method must try/catch
  Future<void> sendResetPasswordLink(final String email) async {
    final route = ServerConfig.getRequestPasswordResetEmailRoute();
    final body = jsonEncode(email);
    final response = await makeAuthenticatedPostRequest(route, body);
    if (response.statusCode != HttpStatus.ok) {
      debugPrint("Error getting password reset email");
      throw Exception(response.body);
    }
  }

  Future<void> initialize() async {
    _token = await _secureStorage.read(key: 'authToken');
    _therapistId = await _secureStorage.read(key: 'therapistId');
    _ownerId = await _secureStorage.read(key: 'ownerId');
    _updateIsLoggedIn();
  }

  /// Sets the login state and updates stored credentials.
  void _updateIsLoggedIn() {
    _isTherapistLoggedIn = _token != null && _therapistId != null;
    _isOwnerLoggedIn = _token != null && _ownerId != null;
    _isGuestLoggedIn = _guestId != null;
    notifyListeners();
  }

  /// Sets the token securely in storage and updates the login state.
  Future<void> setToken(final String? token) async {
    _token = token;
    await _setSecureItem('authToken', token);
    _updateIsLoggedIn();
  }

  /// Sets the therapist ID securely in storage and updates the login state.
  Future<void> setTherapistId(final String? id) async {
    _therapistId = id;
    await _setSecureItem('therapistId', id);
    _updateIsLoggedIn();
  }

  /// Sets the therapist ID securely in storage and updates the login state.
  Future<void> setGuestId(final String? id) async {
    _guestId = id;
    await _setSecureItem('guestTherapistId', id);
    _updateIsLoggedIn();
  }

  Future<void> setOwnerId(final String? id) async {
    _ownerId = id;
    await _setSecureItem('ownerId', id);
    _updateIsLoggedIn();
  }


  /// Helper method to securely store or delete an item.
  Future<void> _setSecureItem(final String key, final String? value) async {
    if (value != null) {
      await _secureStorage.write(key: key, value: value);
    } else {
      await _secureStorage.delete(key: key);
    }
  }

  /// Getters for token, therapistId, and login status.
  String? get token => _token;

  String? get therapistId => _therapistId;

  String? get guestId => _guestId;

  String? get ownerId => _ownerId;

  bool get isLoggedIn => _isTherapistLoggedIn || _isOwnerLoggedIn || _isGuestLoggedIn;

  // Method to register a new Therapist.
  Future<String?> registerTherapist(
      final Therapist therapist, final String password) async {
    try {
      final payload = {
        'email': therapist.email,
        'password': password,
        'fName': therapist.fName,
        'lName': therapist.lName,
        'country': therapist.country ?? '',
        'city': therapist.city ?? '',
        'street': therapist.street ?? '',
        'postalCode': therapist.postalCode ?? '',
        'phone': therapist.phone ?? '',
        'profession': therapist.profession ?? '',
        'major': therapist.major ?? '',
        'yearsExperienceInHippotherapy':
            therapist.yearsExperienceInHippotherapy ?? 0,
        'ownerId': therapist.ownerId,
        'verified': therapist.verified,
        'referral': therapist.referral,
      };

      final route = ServerConfig.getRegisterTherapistRoute();
      final body = jsonEncode(payload);
      final response = await makeAuthenticatedPostRequest(route, body);

      // TODO: check why display error for success
      if (response.statusCode == 200) {
        // Registration successful
        return null;
      } else if (response.statusCode == 202) {
        return "202";
      } else if (response.statusCode == 406) {
        return "406";
      } else if (response.statusCode == 201) {
        return "201";
      } else {
        final responseData = jsonDecode(response.body);
        if (responseData['Message'] != null) {
          return "Registration failed: ${responseData['Message']}";
        } else if (responseData['Error'] != null) {
          return "Registration failed: ${responseData['Error']}";
        } else {
          // Fallback for unknown response
          return "Registration failed: ${response.body}";
        }
      }
    } catch (e) {
      debugPrint("Error during registration: $e");
      return "Registration failed due to a network or unexpected error. Please try again.";
    }
  }

  Future<String?> registerOwner(
      final Owner owner, final String password) async {
    try {
      final payload = {
        'email': owner.email,
        'password': password,
        'fName': owner.fName,
        'lName': owner.lName
      };

      final route = ServerConfig.getRegisterOwnerRoute();
      final body = jsonEncode(payload);
      final response = await makeAuthenticatedPostRequest(route, body);

      // TODO: check why displays error for success
      if (response.statusCode == 200) {
        // Registration successful
        return null;
      } else {
        final responseData = jsonDecode(response.body);
        if (responseData['Message'] != null) {
          return "Registration failed: ${responseData['Message']}";
        } else if (responseData['Error'] != null) {
          return "Registration failed: ${responseData['Error']}";
        } else {
          // Fallback for unknown response
          return "Registration failed: ${response.body}";
        }
      }
    } catch (e) {
      debugPrint("Error during registration: $e");
      return "Registration failed due to a network or unexpected error. Please try again.";
    }
  }

  // Login method for user authentication
  Future<Map<String, dynamic>?> login(
      final String email, final String password, final Role role) async {
    try {
      final payload = {'email': email, 'password': password};

      String route = "";
      if (role == Role.THERAPIST) {
        route = ServerConfig.getLoginTherapistRoute();
      } else if (role == Role.OWNER) {
        route = ServerConfig.getLoginOwnerRoute();
      } else {
        debugPrint("ERROR: does not have a role of owner or therapist");
      }

      final body = jsonEncode(payload);
      final response = await makeAuthenticatedPostRequest(route, body);

      if (response.statusCode == 200) {
        final responseData = jsonDecode(response.body);
        await setToken(responseData['token']);
        final loginDetails = {'token': responseData['token']};
        if (role == Role.THERAPIST) {
          await setTherapistId(responseData['userId']);
          loginDetails['therapistId'] = responseData['userId'];
        }
        if (role == Role.OWNER) {
          await setOwnerId(responseData['userId']);
          loginDetails['ownerId'] = responseData['userId'];
        }
        _updateIsLoggedIn();

        return loginDetails;
      } else {
        final responseData = jsonDecode(response.body);
        return {
          'errorType': responseData['errorType'] ?? 'General',
          'message': responseData['message'] ?? 'Login failed.'
        };
      }
    } catch (e) {
      debugPrint("Login exception: $e");
      return {
        'errorType': 'Exception',
        'message': 'An unexpected error occurred. Please try again.'
      };
    }
  }

  // Method to get a therapist's profile information
  Future<Therapist?> getTherapistInfo(final String therapistId) async {
    try {
      final route = ServerConfig.getTherapistByIdRoute(therapistId);
      final response = await makeAuthenticatedGetRequest(route);

      if (response.statusCode == 200) {
        final Map<String, dynamic> jsonResponse = jsonDecode(response.body);
        return Therapist.fromJson(jsonResponse);
      } else if (response.statusCode == 404) {
        debugPrint('Therapist not found for ID: $therapistId');
      } else {
        debugPrint('Failed to fetch therapist: ${response.body}');
      }
    } catch (e) {
      debugPrint('Error fetching therapist: $e');
    }

    return null;
  }

  Future<Owner?> getOwnerInfo(final String ownerId) async {
    try {
      final route = ServerConfig.getOwnerByIdRoute(ownerId);
      final response = await makeAuthenticatedGetRequest(route);

      if (response.statusCode == 200) {
        final Map<String, dynamic> jsonResponse = jsonDecode(response.body);
        return Owner.fromJson(jsonResponse);
      } else if (response.statusCode == 404) {
        debugPrint('Owner not found for ID: $ownerId');
      } else {
        debugPrint('Failed to fetch owner: ${response.body}');
      }
    } catch (e) {
      debugPrint('Error fetching owner: $e');
    }

    return null;
  }

  // sends request to backend to generateReferral along with therapistID and email
  Future<http.Response?> generateReferral(
      final String ownerId, final String email) async {
    try {
      final payload = {
        'email': email,
        'ownerId': ownerId,
      };

      final body = jsonEncode(payload);
      final route = ServerConfig.getGenerateReferralRoute();
      final response = await makeAuthenticatedPostRequest(route, body);
      // return the request
      return response;
    } catch (e) {
      debugPrint("Error generating referral: $e");
    }

    // return the request
    return null;
  }

  /// Method to logout
  Future<void> logout() async {
    // TODO: add check in here for if they are logged in as Owner or Therapist
    await setToken(null);
    await setTherapistId(null);
    await setOwnerId(null);
    await setGuestId(null);
    notifyListeners();
  }

  /// Method to check user's login status
  Future<bool> checkLoginStatus() async {
    _token = await _secureStorage.read(key: 'authToken');
    _therapistId = await _secureStorage.read(key: 'therapistId');
    _ownerId = await _secureStorage.read(key: 'ownerId');
    _isTherapistLoggedIn = _token != null && _therapistId != null;
    _isOwnerLoggedIn = _token != null && _ownerId != null;
    _isGuestLoggedIn = _guestId != null;
    return _isTherapistLoggedIn || _isOwnerLoggedIn || _isGuestLoggedIn; // Checks therapist first
  }

  /// Make an authenticated GET request using stored token
  /// optionally pass in query parameters
  Future<http.Response> makeAuthenticatedGetRequest(final String route,
      [final Map<String, String>? queryParameters]) async {
    debugPrint('trying GET to $route');

    // Create a Uri with the route and query parameters
    final uri = Uri.parse(route).replace(queryParameters: queryParameters);

    return await http.get(uri, headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    });
  }

  /// Logs into the app as guest
  /// returns true if connected to backend and validated email
  /// false otherwise
  Future<bool> loginAsGuest(final String email, final void setState) async {
    // make request to backend, if OK return true
    final String route = ServerConfig.getLogGuestLoginRoute();
    final body = jsonEncode(email);
    final res = await makeAuthenticatedPostRequest(route, body);

    if (res.statusCode == 200) {
      // Return if it was successfully connected and validated in the backend

      // set logged in as therapist, use email as unique therapistId
      //setTherapistId(email);
      setGuestId(email);
      return true;
    } else {
      return false;
    }
  }

  /// Make an authenticated POST request to the route with a body
  Future<http.Response> makeAuthenticatedPostRequest(
      final String route, final dynamic body) async {
    debugPrint('trying POST to $route');
    return await http.post(
      Uri.parse(route),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: body,
    );
  }

  /// Makes an authenticated PUT request to the route with a body
  Future<http.Response> makeAuthenticatedPutRequest(
      final String route, final dynamic body) async {
    debugPrint('trying PUT to $route');
    return await http.put(
      Uri.parse(route),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: body,
    );
  }

  /// Makes an authenticated DELETE request to the route with stored token
  Future<http.Response> makeAuthenticatedDeleteRequest(
      final String route) async {
    debugPrint('trying DELETE to $route');
    return await http.delete(Uri.parse(route), headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
    });
  }
}
