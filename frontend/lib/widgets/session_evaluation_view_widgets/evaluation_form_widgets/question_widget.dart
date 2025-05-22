import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:frontend/models/pose.dart';
import 'package:localstore/localstore.dart';

class QuestionWidget extends StatefulWidget {
  final String category;
  final List<FormBuilderChipOption<dynamic>> options;
  final Function(String, String?) onStateChanged;
  final String? initialImage;
  final GlobalKey<FormBuilderState> formKey;

  const QuestionWidget({
    required this.formKey,
    required this.category, // category
    required this.options, // list of options
    required this.onStateChanged, // function to change image based on state
    this.initialImage, // starting image
    super.key,
  });

  @override
  _QuestionWidgetState createState() => _QuestionWidgetState();
}

class _QuestionWidgetState extends State<QuestionWidget> {
  final db = Localstore.instance;
  String? selectedImgPath;

  @override
  void initState() {
    super.initState();

    selectedImgPath = widget.initialImage;
  }

  // detecting state of selected images to update the large image
  void _selectedImage(final String category, final int? value) {
    setState(() {
      if (value != null) {
        selectedImgPath = PoseType.values
            .firstWhere((final pose) =>
                pose.category.toString() ==
                    "PoseGroup.${category.toUpperCase()}" &&
                pose.value == value)
            .imgPath;
      } else {
        selectedImgPath = null;
      }
      widget.onStateChanged(widget.category, selectedImgPath);
    });
  }

  @override
  Widget build(final BuildContext context) {
    return Column(
      children: [
        SizedBox(
          width: 100,
          height: 100,
          child: selectedImgPath != null
              ? Image.asset(selectedImgPath!)
              : const Icon(Icons.question_mark),
        ),
        FormBuilderChoiceChips(
          shape: const LinearBorder(),
          // hide the border of the images
          avatarBorder:
              const Border(bottom: BorderSide(width: 50, color: Colors.green)),
          visualDensity: VisualDensity.compact,
          labelPadding: const EdgeInsets.all(0),
          showCheckmark: false,
          selectedColor: Colors.green,
          alignment: WrapAlignment.center,
          decoration: const InputDecoration(
            contentPadding: EdgeInsets.symmetric(horizontal: 50, vertical: 10),
          ),
          name: widget.category,
          options: widget.options,
          onChanged: (final value) {
            _selectedImage(widget.category, value);
          },
          validator: FormBuilderValidators.required(
              errorText: "${widget.category} is required"),
        ),
      ],
    );
  }
}
