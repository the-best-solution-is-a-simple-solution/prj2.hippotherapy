import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/patient_controller.dart';

/// Patient Name field that contains the unique names of all the patients
class PatientNameField extends StatefulWidget {
  final GlobalKey<FormBuilderState> formKey;
  final ValueNotifier<bool> notifier; // notifier for the export all checkbox

  const PatientNameField(
      {super.key, required this.formKey, required this.notifier});

  @override
  State<PatientNameField> createState() => _PatientNameFieldState();
}

class _PatientNameFieldState extends State<PatientNameField> {
  bool _isEnabled = false;
  List<String> nameOptions = []; // list of unique names of patients

  void initList() async {
    // initalizes the controller and updates the list
    setNames();
  }

  void setNames() async {
    final AuthController authController = AuthController();
    try {
      nameOptions = await PatientController.getUniqueNamesOfPatients(
          authController.ownerId!);
      nameOptions.sort();
      if (mounted) {
        setState(() {});
      }
    } catch (e) {}
    // updates the widget so that it can display the unique state
  }

  @override
  void initState() {
    super.initState();
    initList();
    widget.notifier.addListener(() {
      // listener for the export All checkbox
      if (widget.notifier.value) {
        setState(() {
          // make current checkbox false if export all fields is true
          _isEnabled = false;
          widget.formKey.currentState?.fields['nameOfPatient']?.didChange(null);
        });
      }
    });
  }

  @override
  Widget build(final BuildContext context) {
    return Row(
      children: [
        Checkbox(
          // initalization and overall apperance of checkbox
          key: const Key("nameOfPatientBox"),
          value: _isEnabled,
          onChanged: (final bool? value) {
            setState(() {
              _isEnabled = value ?? false;
              widget.formKey.currentState?.fields['nameOfPatient']
                  ?.didChange(null);
              widget.formKey.currentState?.fields['all']
                  ?.didChange(false); // update export all field to be false.
              if (_isEnabled) {
                // if current checkbox is enabled, make export all false
                widget.notifier.value = false;
              }
            });
          },
        ),
        const SizedBox(width: 10),
        Expanded(
          key: const Key("nameOfPatient"),
          child: FormBuilderDropdown(
            name: 'nameOfPatient',
            enabled: _isEnabled,
            decoration: const InputDecoration(
              labelText: 'Name Of Patient',
            ),
            items: nameOptions
                .map((final con) => DropdownMenuItem(
              // map each item in the list to be an option in the select box
                alignment: AlignmentDirectional.center,
                key: Key(con.replaceAll(',', "")),
                value: con,
                child: Text(con)))
                .toList(),
            valueTransformer: (final val) => val
                ?.toString(), // transform each value object into its string representation to store in formkey
          ),
        ),
      ],
    );
  }
}
