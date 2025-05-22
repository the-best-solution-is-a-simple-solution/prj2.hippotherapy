import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:frontend/controllers/patient_session_controller.dart';

/// The location field select box that is popualted with unique locations from all the sessions
class LocationField extends StatefulWidget {
  final GlobalKey<FormBuilderState> formKey;
  final ValueNotifier<bool> notifier;

  const LocationField(
      {super.key, required this.formKey, required this.notifier});

  @override
  State<LocationField> createState() => _LocationFieldState();
}

class _LocationFieldState extends State<LocationField> {
  bool _isEnabled = false;
  List<String> locationOptions =
      []; // holds the unique locations to populate the select box

  void initList() async {
    // initalizes the controller and grabs the locationOptions
    setConditions();
  }

  void setConditions() async {
    try {
      locationOptions =
          await PatientSessionController.fetchSessionUniqueLocations();
    } catch (e) {
      //nothing
    }

    locationOptions.sort();
    if (mounted) {
      setState(
          () {}); // update state so the widget can refresh and display the newly acquired populated locations.
    }
  }

  @override
  void initState() {
    super.initState();
    initList();
    widget.notifier.addListener(() {
      // listener for the exportAll checkbox
      if (widget.notifier.value) {
        setState(() {
          // if it is true, disable current checkbox and make value null
          _isEnabled = false;
          widget.formKey.currentState?.fields['location']?.didChange(null);
        });
      }
    });
  }

  @override
  Widget build(final BuildContext context) {
    return Row(
      children: [
        Checkbox(
          // intitalization and apperance of checkbox
          key: const Key("locationBox"),
          value: _isEnabled,
          onChanged: (final bool? value) {
            setState(() {
              _isEnabled = value ?? false;
              widget.formKey.currentState?.fields['location']?.didChange(null);
              widget.formKey.currentState?.fields['all']?.didChange(false);
              if (_isEnabled) {
                widget.notifier.value = false;
              }
            });
          },
        ),
        const SizedBox(width: 10),
        Expanded(
          // expanded to take over what left over space the checkbox hasn't taken over yet
          child: FormBuilderDropdown(
            key: const Key("location"),
            name: 'location',
            enabled: _isEnabled,
            decoration: const InputDecoration(
              labelText: 'Location',
            ),
            items: locationOptions
                .map((final location) => DropdownMenuItem(
                    // map each item in the array to be an option in the select box
                    key: Key(location),
                    alignment: AlignmentDirectional.center,
                    value: location,
                    child: Text(location)))
                .toList(),
            valueTransformer: (final val) => val
                ?.toString(), // transform each value object to its string representation to write to formKey
          ),
        ),
      ],
    );
  }
}
