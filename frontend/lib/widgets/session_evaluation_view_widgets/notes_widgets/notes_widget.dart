import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:fluttertagger/fluttertagger.dart';
import 'package:frontend/controllers/patient_evaluation_controller.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/notes_widgets/search_result_overlay.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/notes_widgets/search_view_model.dart';

class NotesWidget extends StatefulWidget {
  final GlobalKey<FormBuilderState> formKey;
  final List<String> validTags;

  const NotesWidget({
    required this.formKey,
    required this.validTags,
    super.key,
  });

  @override
  _NotesWidgetState createState() => _NotesWidgetState();
}

class _NotesWidgetState extends State<NotesWidget>
    with TickerProviderStateMixin {
  late AnimationController _animationController;
  late Animation<Offset> _animation;
  List<String> userTags = [];
  double overlayHeight = 300;
  Text tagListExclusionNote = const Text(
      'No tags preventing this evaluation from being viewed on the graph.');

  late final _controller = FlutterTaggerController();
  late final _focusNode = FocusNode();

  @override
  void initState() {
    super.initState();
    _animationController = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 300));

    _animation = Tween<Offset>(begin: const Offset(0, 0.5), end: Offset.zero)
        .animate(CurvedAnimation(
            parent: _animationController, curve: Curves.easeInOut));

    fetchUserTagList(null);
  }

  @override
  void dispose() {
    _animationController.dispose();
    _focusNode.dispose();
    _controller.dispose();
    super.dispose();
  }

  void fetchUserTagList(String? value) async {
    // If the value is null, try to get the value from the form key
    // If the form key is null, set it to ""
    value ??= widget.formKey.currentState?.value['notes'] ?? "";
    userTags =
        await PatientEvaluationController.getEvaluationsTagsFromNotes(value);

    // Need setState because otherwise it won't load
    setState(() {
      tagListExclusionNote = userTags.isNotEmpty
          ? Text(
              'Tags preventing this evaluation from being viewed on graph tab: $userTags',
              style: const TextStyle(color: Colors.redAccent))
          : const Text(
              'No tags preventing this evaluation from being viewed on the graph.');
    });
  }

  @override
  Widget build(final BuildContext context) {
    return Column(children: [
      SizedBox(
          height: 200,
          child: Center(
              child: Text(
                  "Evaluations can be excluded from analysis by using 'tags'.\n"
                  "Ex. entering #sick will exclude the evaluation from showing up on the graph.\n"
                  "The list of valid tags that will exclude an evaluation is as follows:\n${widget.validTags}",
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.deepOrangeAccent)))),
      FlutterTagger(
        triggerStrategy: TriggerStrategy.eager,
        controller: _controller,
        animationController: _animationController,
        onSearch: (final query, final triggerChar) {
          if (triggerChar == "#") {
            searchViewModel.searchTags(query);
          }
        },
        triggerCharacterAndStyles: const {
          "#": TextStyle(color: Colors.blueAccent),
        },
        tagTextFormatter: (final _, final tag, final triggerCharacter) {
          return "$triggerCharacter$tag";
        },
        overlayHeight: overlayHeight,
        overlay: SearchResultOverlay(
          animation: _animation,
          tagController: _controller,
        ),
        builder: (final context, final textFieldKey) {
          return Center(
              child: Column(children: [
            SizedBox(
                key: const Key('sizedbox_formbuilder_notes'),
                width: 500,
                child: FormBuilderTextField(
                    key: textFieldKey,
                    name: 'notes',
                    maxLines: 15,
                    maxLength: 2048,
                    controller: _controller,
                    focusNode: _focusNode,
                    style: const TextStyle(
                      color: Colors.black,
                      fontWeight: FontWeight.w400,
                      fontSize: 16,
                    ),
                    cursorColor: Colors.redAccent,
                    onChanged: (final String? value) {
                      fetchUserTagList(value);
                    })),
            SizedBox(width: 500, child: tagListExclusionNote)
          ]));
        },
      )
    ]);
  }
}
