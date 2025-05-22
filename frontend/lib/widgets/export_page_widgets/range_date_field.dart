import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:frontend/controllers/patient_session_controller.dart';
import 'package:intl/intl.dart';

/// Range date field where the therapist can select the date range between two dates or only one date
class RangeDateField extends StatefulWidget {
  final GlobalKey<FormBuilderState> formKey;
  final ValueNotifier<bool> notifier; // notifier for the export all checkbox
  const RangeDateField(
      {super.key, required this.formKey, required this.notifier});

  @override
  State<RangeDateField> createState() => _RangeDateFieldState();
}

class _RangeDateFieldState extends State<RangeDateField> {
  bool _isEnabled = false;
  int lowestYear = DateTime.now().year; // default value will be the year

  void initList() async {
    // initalizes the lowestYear in db and controller
    setLowestHighestDateRange();
  }

  void setLowestHighestDateRange() async {
    try {
      lowestYear = await PatientSessionController.fetchLowestDate();
    } catch (e) {
      //nothing
    }
    if (mounted) {
      setState(() {});
    } // update state so that the lowest year can render on the date range
  }

  @override
  void initState() {
    super.initState();
    initList();
    widget.notifier.addListener(() {
      if (widget.notifier.value) {
        // listener for the notifier that export all checkbox
        setState(() {
          // if enabled, turn current checkbox false.
          _isEnabled = false;
          widget.formKey.currentState?.fields['dateNow']?.didChange(null);
        });
      }
    });
  }

  @override
  Widget build(final BuildContext context) {
    return Row(
      children: [
        Checkbox(
          // initlization and apperance of checkbox
          key: const Key("dateNowBox"),
          value: _isEnabled,
          onChanged: (final bool? value) {
            setState(() {
              _isEnabled = value ?? false;
              widget.formKey.currentState?.fields['dateNow']?.didChange(null);
              widget.formKey.currentState?.fields['all']?.didChange(false);
              if (_isEnabled) {
                // if current checkbox enabled, turn export all to false.
                widget.notifier.value = false;
              }
            });
          },
        ),
        const SizedBox(width: 10),
        Expanded(
          key: const Key("dateNow"),
          child: FormBuilderDateRangePicker(
            name: 'dateNow',
            enabled: _isEnabled,
            firstDate: DateTime(lowestYear),
            lastDate: DateTime.now(),
            initialValue: const bool.fromEnvironment('FLUTTER_TEST')
                ? DateTimeRange(
                    start: DateTime.parse("2021-11-20"),
                    end: DateTime.parse("2021-11-20"))
                : null,
            decoration: const InputDecoration(
              labelText: "Date Range",
            ),
            valueTransformer: (final value) {
              // custom value transformer to easily query date value
              if (value == null) {
                return null;
              }
              // translate format to easily querable
              final DateFormat formatter = DateFormat('yyyy-MM-dd');
              return '${formatter.format(value.start)},${formatter.format(value.end)}';
            },
          ),
        ),
      ],
    );
  }
}
