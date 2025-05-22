import 'package:flutter/material.dart';
import 'package:fluttertagger/fluttertagger.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/notes_widgets/search_view_model.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/notes_widgets/tag_list_view_widget.dart';

class SearchResultOverlay extends StatelessWidget {
  const SearchResultOverlay({
    super.key,
    required this.tagController,
    required this.animation,
  });

  final FlutterTaggerController tagController;
  final Animation<Offset> animation;

  @override
  Widget build(final BuildContext context) {
    return ValueListenableBuilder<SearchResultView>(
      valueListenable: searchViewModel.activeView,
      builder: (final _, final view, final __) {
        if (view == SearchResultView.tag) {
          return TagListView(
            taggerController: tagController,
            animation: animation,
          );
        }
        return const SizedBox();
      },
    );
  }
}
