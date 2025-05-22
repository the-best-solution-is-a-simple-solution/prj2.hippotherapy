import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:fluttertagger/fluttertagger.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/notes_widgets/search_view_model.dart';

// https://github.com/Crazelu/fluttertagger/blob/main/example/lib/views/widgets/user_list_view.dart

class TagListView extends StatelessWidget {
  const TagListView(
      {super.key, required this.taggerController, required this.animation});

  final FlutterTaggerController taggerController;
  final Animation<Offset> animation;

  @override
  Widget build(final BuildContext context) {
    return SlideTransition(
      position: animation,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 2),
        decoration: BoxDecoration(
          borderRadius: const BorderRadius.only(
            topLeft: Radius.circular(20),
            topRight: Radius.circular(20),
          ),
          color: Colors.white,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(.2),
              offset: const Offset(0, -20),
              blurRadius: 20,
              spreadRadius: 2,
            ),
          ],
        ),
        child: Material(
          color: Colors.transparent,
          child: ValueListenableBuilder<bool>(
            valueListenable: searchViewModel.loading,
            builder: (final _, final loading, final __) {
              return ValueListenableBuilder<List<String>>(
                valueListenable: searchViewModel.tags,
                builder: (final _, final tags, final __) {
                  return Column(
                    children: [
                      const SizedBox(height: 8),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceAround,
                        children: [
                          const Spacer(),
                          Text(
                            "Tags",
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                          const Spacer(),
                          IconButton(
                            onPressed: taggerController.dismissOverlay,
                            icon: const Icon(Icons.close),
                          ),
                        ],
                      ),
                      if (loading && tags.isEmpty) ...{
                        const Center(
                          heightFactor: 8,
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Text(
                                "Loading",
                              ),
                              SizedBox(width: 10),
                              CupertinoActivityIndicator(radius: 10),
                            ],
                          ),
                        )
                      },
                      if (!loading && tags.isEmpty)
                        const Center(
                          heightFactor: 8,
                          child: Text("No tag found"),
                        ),
                      if (tags.isNotEmpty)
                        Expanded(
                          child: ListView.builder(
                            padding: EdgeInsets.zero,
                            itemCount: tags.length,
                            itemBuilder: (final _, final index) {
                              final tag = tags[index];
                              return ListTile(
                                leading: Container(
                                  height: 40,
                                  width: 40,
                                  decoration: BoxDecoration(
                                    shape: BoxShape.circle,
                                    border: Border.all(
                                        color: Colors.lightBlueAccent),
                                  ),
                                  alignment: Alignment.center,
                                  child: const Text(
                                    "#",
                                    style: TextStyle(color: Colors.blueAccent),
                                  ),
                                ),
                                title: Text(tag),
                                onTap: () {
                                  taggerController.addTag(
                                    id: tag,
                                    name: tag,
                                  );
                                },
                              );
                            },
                          ),
                        ),
                    ],
                  );
                },
              );
            },
          ),
        ),
      ),
    );
  }
}
