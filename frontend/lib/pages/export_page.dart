import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:frontend/controllers/export_controller.dart';
import 'package:frontend/widgets/export_page_widgets/condition_field.dart';
import 'package:frontend/widgets/export_page_widgets/export_all_checkbox.dart';
import 'package:frontend/widgets/export_page_widgets/location_field.dart';
import 'package:frontend/widgets/export_page_widgets/patient_name_field.dart';
import 'package:frontend/widgets/export_page_widgets/range_date_field.dart';
import 'package:frontend/widgets/helper_widgets/alert_dialog_box.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:to_csv/to_csv.dart' as export_csv;

/// Export page in which the therapist can select filters and export by data.
class ExportPage extends StatefulWidget {
  static const String RouteName = '/export';
  const ExportPage({super.key});

  @override
  State<ExportPage> createState() => _ExportPageState();
}

class _ExportPageState extends State<ExportPage> {
  // value notifier which will rebuild all checkbox widgets based on
  // the boolean export all checkbox.
  final ValueNotifier<bool> exportAllNotifier = ValueNotifier(true);
  final _formKey = GlobalKey<
      FormBuilderState>(); // the state of the form, which contains all the values

  @override
  Widget build(final BuildContext context) {
    // initialize the drawer widget
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: const Text("Export"),
      ),
      drawer: const HippoAppDrawer(),
      body: Center(
        // where content of export page begins
        child: Column(children: [
          const Text(
            "Filter By: ",
            style: TextStyle(fontSize: 30, fontWeight: FontWeight.bold),
          ),
          FormBuilder(
              key: _formKey,
              onChanged: () {
                _formKey.currentState!.save();
                debugPrint(_formKey.currentState!.value.toString());
              },
              initialValue: const {
                'all': true,
              },
              child: Padding(
                padding: const EdgeInsets.all(15),
                child: Column(
                  children: <Widget>[
                    const SizedBox(height: 30),
                    // padding between the inputs
                    PatientNameField(
                        formKey: _formKey, notifier: exportAllNotifier),
                    const SizedBox(height: 75),
                    RangeDateField(
                        formKey: _formKey, notifier: exportAllNotifier),
                    const SizedBox(height: 75),
                    ConditionField(
                        formKey: _formKey, notifier: exportAllNotifier),
                    const SizedBox(height: 75),
                    LocationField(
                        formKey: _formKey, notifier: exportAllNotifier),
                    const SizedBox(height: 75),
                    ExportAllCheckbox(notifier: exportAllNotifier),
                    const SizedBox(height: 75),
                    ElevatedButton(
                        onPressed: () => {
                          if (_formKey.currentState?.saveAndValidate() ??
                              false)
                            {
                              debugPrint(
                                  _formKey.currentState?.value.toString())
                            },
                          exportInfo(_formKey.currentState, context)
                        },
                        child: const Text('Submit')),
                  ],
                ),
              ))
        ]),
      ),
    );
  }
}

void exportInfo(
    final FormBuilderState? formInfo, final BuildContext context) async {
  // These are the headers that the csv will have.
  final List<String> header = [
    'Name',
    'Age',
    'Weight',
    'Height',
    'Condition',
    'Date Taken',
    'Location',
    'Eval Type',
    'Lumbar',
    'HipFlex',
    'HeadAnt',
    'HeadLat',
    'KneeFlex',
    'Pelvic',
    'PelvicTilt',
    'Thoracic',
    'Trunk',
    'TrunkInclination',
    'ElbowExtension',
  ];

  // initalizing the values from the form and giving them an empty string if they are null for
  // easier processing of paramteres when pushed to the fetchFilteredRecords.
  final String patientName = formInfo?.value['nameOfPatient'] ?? '';
  final String dateNow = formInfo?.value['dateNow'] ?? '';
  final String condition = formInfo?.value['condition'] ?? '';
  final String location = formInfo?.value['location'] ?? '';
  final bool all = formInfo?.value['all'] ?? false;

  // checking if all fields are null and that all is false. meaning user has not selected any option
  if (!all &&
      patientName == "" &&
      dateNow == "" &&
      condition == "" &&
      location == "") {
    alertDialogBox(context, "Export Unsucessful, please filter by option/s.");
    return;
  }

  List<List<String>> listOfLists = [];
  // initializing list to fetch all filtered records

  try {
    listOfLists = await ExportController.fetchFilteredRecords(
        patientName, condition, location, dateNow);
  } catch (e) {
    alertDialogBox(context, "Export Unsuccessful, no records found.");
    return;
  }

  if (listOfLists.isEmpty) {
    alertDialogBox(context, "Export Unsuccessful, no records found.");
  } else {
    // if list is not empty, it has records, and now we export them. using to_csv.
    if (!const bool.fromEnvironment('FLUTTER_TEST')) {
      // for checking if it is NOT a test
      await export_csv.myCSV(header, listOfLists,
          setHeadersInFirstRow: true, includeNoRow: true, sharing: false);
    }

    alertDialogBox(context, "Export Successful");
  }
}
