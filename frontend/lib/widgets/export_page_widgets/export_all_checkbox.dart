import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';

/// Export All checkbox field that a therapist can check if they want to export all fields.
class ExportAllCheckbox extends StatelessWidget {
  final ValueNotifier<bool> notifier; // notifier for the export all checkbox

  const ExportAllCheckbox({
    super.key,
    required this.notifier,
  });

  @override
  Widget build(final BuildContext context) {
    return Row(
      // just using row to align itself in the center
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        SizedBox(
          key: const Key("exportAll"),
          width: 300,
          child: ValueListenableBuilder<bool>(
            // it will update everytime the notifier is changed by the other fields.
            valueListenable: notifier,
            builder: (final BuildContext context, final bool value,
                final Widget? child) {
              // rebuild itself starting from here
              return FormBuilderField<bool>(
                // custom form builder from the flutter form builder widget
                name: "all", // name of the all formKey
                builder: (final FormFieldState<bool?> field) {
                  // passing in the formKey state
                  return InputDecorator(
                    // the overall apperance of the widget
                    decoration: const InputDecoration(border: InputBorder.none),
                    child: CheckboxListTile(
                      key: const Key("exportAllBox"),
                      value: value,
                      // assigning its value based on the notifer value
                      onChanged: (final newValue) {
                        // newValue is the local value that the exportAllFields contains
                        notifier.value = newValue ?? false;
                        field.didChange(
                            newValue); // change the "all" field in formKey if checked
                      },
                      title: const Text(
                        "Export All Fields",
                        style: TextStyle(fontSize: 20),
                      ),
                    ),
                  );
                },
              );
            },
          ),
        ),
      ],
    );
  }
}
