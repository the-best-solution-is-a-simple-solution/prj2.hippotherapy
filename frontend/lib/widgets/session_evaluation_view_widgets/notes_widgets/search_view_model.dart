import 'package:flutter/material.dart';
import 'package:frontend/controllers/patient_evaluation_controller.dart';

final searchViewModel = SearchViewModel();

enum SearchResultView { tag, none }

class SearchViewModel {
  late final ValueNotifier<List<String>> _tags = ValueNotifier([]);
  ValueNotifier<List<String>> get tags => _tags;

  late final ValueNotifier<bool> _loading = ValueNotifier(false);
  ValueNotifier<bool> get loading => _loading;

  late final ValueNotifier<SearchResultView> _activeView =
      ValueNotifier(SearchResultView.none);
  ValueNotifier<SearchResultView> get activeView => _activeView;

  void _setLoading(final bool val) {
    if (val != _loading.value) {
      _loading.value = val;
    }
  }

  Future<void> searchTags(String query) async {
    final List<String> validTags =
        await PatientEvaluationController.getEvaluationTagList();

    _activeView.value = SearchResultView.tag;
    if (query.isEmpty) {
      return;
    }

    query = query.toLowerCase().trim();

    _tags.value = [];

    _setLoading(true);

    await Future.delayed(const Duration(milliseconds: 250));

    final result = (validTags)
        .where((final tag) => tag.toLowerCase().contains(query))
        .toList();
    _tags.value = [...result];
    _setLoading(false);
  }
}
