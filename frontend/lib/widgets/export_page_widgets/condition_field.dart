import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/patient_controller.dart';

/// The condition field which is a select box with all the unique conditions of patients
class ConditionField extends StatefulWidget {
  final GlobalKey<FormBuilderState> formKey;
  final ValueNotifier<bool> notifier;

  const ConditionField(
      {super.key, required this.formKey, required this.notifier});

  @override
  State<ConditionField> createState() => _ConditionFieldState();
}

class _ConditionFieldState extends State<ConditionField> {
  List<String> conditionOptions = []; // the list of conditionOptions
  bool _isEnabled = false; // the state of the checkbox beisde the field

  // initalizes the controller and grabs the unique conditions from the backend
  // to populate the select box
  void initList() async {
    setConditions();
  }

  // grabbing the unique conditions from the controller and updating the state
  // to refresh the widget with the values
  void setConditions() async {
    final AuthController authController = AuthController();
    try {
      conditionOptions = await PatientController.getUniqueConditionsOfPatients(
          authController.ownerId!);
      conditionOptions.sort();
      if (mounted) {
        setState(() {});
      }
      ;
    } catch (e) {}
  }

  @override // called everytime the state is rebuilt
  void initState() {
    super.initState();
    initList();
    widget.notifier.addListener(() {
      // add a listener that will notify the export all checkbox to change its value
      if (widget.notifier.value) {
        setState(() {
          // if the export all checkbox is true, it will make the condition checkbox false.
          _isEnabled = false;
          widget.formKey.currentState?.fields['condition']?.didChange(null);
        });
      }
    });
  }

  @override
  Widget build(final BuildContext context) {
    return Row(
      children: [
        Checkbox(
          // the apperance and initalization of the checkbox
          key: const Key("conditionBox"),
          value: _isEnabled,
          onChanged: (final bool? value) {
            setState(() {
              // everytime the condition checkbox is set to true, it will notify the export all to be false.
              // and updating the formkey state to be of that value.
              _isEnabled = value ?? false;
              widget.formKey.currentState?.fields['condition']?.didChange(null);
              widget.formKey.currentState?.fields['all']?.didChange(false);
              if (_isEnabled) {
                widget.notifier.value = false;
              }
            });
          },
        ),
        const SizedBox(width: 10),
        Expanded(
          key: const Key("condition"),
          child: FormBuilderDropdown(
            // the apperance and initalization of the select box
            name: 'condition',
            enabled: _isEnabled,
            decoration: const InputDecoration(
              labelText: 'Condition',
            ),
            items: conditionOptions
                .map((final con) => DropdownMenuItem(
              // map each item in the list to be of an option
                alignment: AlignmentDirectional.center,
                value: con,
                child: Text(con)))
                .toList(),
            valueTransformer: (final val) => val
                ?.toString(), // transform each val object to its string representation to save to formKey
          ),
        ),
      ],
    );
  }
}
