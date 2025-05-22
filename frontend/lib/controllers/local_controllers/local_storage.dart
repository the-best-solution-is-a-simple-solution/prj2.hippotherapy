import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// A class to save and read from local secure storage
class AppLocalStorage {
  /// All instances created access the same storage
  static const FlutterSecureStorage _storage = FlutterSecureStorage();

  /// Helper method to securely store or delete an item.
  Future<void> _setSecureItem(final String key, final String? value) async {
    if (value != null) {
      await _storage.write(key: key, value: value);
    } else {
      await _storage.delete(key: key);
    }
  }

  // Future<void> saveDocument(final String key, final String value) async {
  //   await _storage.write(key: key, value: value);
  // }
  //
  // Future<void> deleteDocument(final String key) async {
  //   await _storage.delete(key: key);
  // }

  /// Store collection references (list of document IDs in a collection)
  /// returns true if succeeded, false if it already existed
  static Future<bool> addToCollection(final String collectionKey, final String docId) async {
    // Read out existing list
    final existingIdsJson = await _storage.read(key: collectionKey) ?? '[]';

    // Convert to list of strings
    final List<String> ids = List<String>.from(jsonDecode(existingIdsJson));

    // Add it if it does not exist
    if (!ids.contains(docId)) {
      ids.add(docId);
      await _storage.write(key: collectionKey, value: jsonEncode(ids));
      return true;
    }
    // Failed to add it
    return false;
  }

  /// Get collection documents in a collection
  /// returns a list of strings of ids in that collection
  /// or empty list if nothing in it
  static Future<List<String>> getCollection(final String collectionPath) async {
    final idsJson = await _storage.read(key: collectionPath) ?? '[]';
    final List<String> ids = List<String>.from(jsonDecode(idsJson));

    return ids;
  }




}