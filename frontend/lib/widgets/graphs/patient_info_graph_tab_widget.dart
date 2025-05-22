import 'package:choice/choice.dart';
import 'package:flutter/material.dart';
import 'package:flutter_date_range_picker/flutter_date_range_picker.dart';
import 'package:frontend/models/patient_evaluation_graph_data.dart';
import 'package:frontend/widgets/helper_widgets/icon_message_widget.dart';
import 'package:syncfusion_flutter_charts/charts.dart';

/// A helper class to display graph lines with color, label and the data
class GraphLine {
  final Color color;
  final String label;
  final Function(PatientEvaluationGraphData) patientEvalFunction;

  GraphLine(this.color, this.label, this.patientEvalFunction);
}

// Keys
Key dateRangeKey = const Key('patientInfoPageGraphTabDateRangePickerKey');
Key checkboxKey = const Key('patientInfoGraphTabCheckboxKey');

class PatientInfoGraphTab extends StatefulWidget {
  final List<PatientEvaluationGraphData> patientEvaluationGraphDataList;

  const PatientInfoGraphTab(
      {required this.patientEvaluationGraphDataList, super.key});

  @override
  State<PatientInfoGraphTab> createState() => _PatientInfoGraphTabState();
}

class _PatientInfoGraphTabState extends State<PatientInfoGraphTab> {
  late final List<PatientEvaluationGraphData> graphData;
  late List<PatientEvaluationGraphData> filteredGraphData;
  late bool switchState;
  late String foundEvaluationStatusMessage;
  int excludedEvalsCount = 0;

  // For date range selector
  late DateRange initialDateRange;
  DateRange? selectedDateRange;
  late bool isDateRangeButtonEnabled;

  // For displaying multiple lines on the graph
  var graphLines = <CartesianSeries>[];
  late Widget postureSelectButton;

  List<String> postures = [
    'Head Lat',
    'Head Ant',
    'Elbow Extension',
    'Hip Flex',
    'Knee Flex',
    'Lumbar',
    'Pelvic',
    'Pelvic Tilt',
    'Thoracic',
    'Trunk',
    'Trunk Inclination',
  ];
  late List<ChoiceData<String>> posturesData;

  List<ChoiceData<String>> multipleSelectedPostures = [];
  List<ChoiceData<String>> defaultSelectedPostures = [];

  // Initialize state of widget
  @override
  void initState() {
    super.initState();

    posturesData = postures.map((final posture) {
      return ChoiceData<String>(
        value: posture,
        title: posture,
      );
    }).toList();

    setupPosturesButton();

    foundEvaluationStatusMessage = 'Found 0 evaluations';
    filteredGraphData = [];

    // Enable toggle averages button
    switchState = true;

    // disable date range if no evaluations
    isDateRangeButtonEnabled = false;

    graphData = widget.patientEvaluationGraphDataList;
    if (graphData.isNotEmpty) {
      isDateRangeButtonEnabled = true;

      graphData.sort((final a, final b) => a.dateTaken.compareTo(b.dateTaken));

      // Copy values so as to not delete the data with pass-by-reference
      for (final PatientEvaluationGraphData data in graphData) {
        filteredGraphData.add(data);
      }
      setupDefaults();

      // Add default lines to graph
      filterGraphByDates(initialDateRange);
      updateGraphLines();
    }
  }

  /// Setup the defaults dates, toggle state and display the graph
  void setupDefaults() {
    // Set date start to first session date
    final DateTime minDate = graphData.first.dateTaken;
    // Set last date to last session date
    final DateTime maxDate = graphData.last.dateTaken;

    // DatePicker https://pub.dev/packages/flutter_date_range_picker
    initialDateRange = DateRange(minDate, maxDate);
    selectedDateRange = initialDateRange;
  }

  /// an array of line colors
  List<Color> graphLineColors = [
    Colors.deepPurple,
    Colors.green,
    Colors.blue,
    Colors.yellow,
    Colors.orange,
    Colors.purple,
    Colors.brown,
    Colors.cyan,
    Colors.teal,
    Colors.indigo,
    Colors.grey,
    Colors.pink,
    Colors.black,
  ];

  /// Update the graph lines, adding or removing them based on what
  /// is currently selected
  void updateGraphLines() {
    if (!mounted) {
      return;
    }

    setState(() {
      // VERY IMPORTANT clear all lines
      // MUST ALSO clear the data so the graph does a proper reset
      final List<PatientEvaluationGraphData> temp = [];

      for (final PatientEvaluationGraphData data in filteredGraphData) {
        temp.add(data);
      }

      filteredGraphData.clear();
      filteredGraphData = temp;
      graphLines.clear();

      // If switch is on, display averages lines
      if (switchState) {
        addGraphLine(
            Colors.lime,
            'Right Leaning Averages',
            (final PatientEvaluationGraphData eval) =>
                eval.calculatedPositiveAverage);
        addGraphLine(
            Colors.red,
            'Left Leaning Averages',
            (final PatientEvaluationGraphData eval) =>
                eval.calculatedNegativeAverage);
      }

      int lineColorIndex = 0;

      final Map<String, GraphLine> postureMap = {
        'Head Lat': GraphLine(graphLineColors[lineColorIndex++], 'Head Lat',
            (final PatientEvaluationGraphData eval) => eval.headLat),
        'Head Ant': GraphLine(graphLineColors[lineColorIndex++], 'Head Ant',
            (final PatientEvaluationGraphData eval) => eval.headAnt),
        'Lumbar': GraphLine(graphLineColors[lineColorIndex++], 'Lumbar',
            (final PatientEvaluationGraphData eval) => eval.lumbar),
        'Hip Flex': GraphLine(graphLineColors[lineColorIndex++], 'Hip Flex',
            (final PatientEvaluationGraphData eval) => eval.hipFlex),
        'Knee Flex': GraphLine(graphLineColors[lineColorIndex++], 'Knee Flex',
            (final PatientEvaluationGraphData eval) => eval.kneeFlex),
        'Pelvic': GraphLine(graphLineColors[lineColorIndex++], 'Pelvic',
            (final PatientEvaluationGraphData eval) => eval.pelvic),
        'Pelvic Tilt': GraphLine(
            graphLineColors[lineColorIndex++],
            'Pelvic Tilt',
            (final PatientEvaluationGraphData eval) => eval.pelvicTilt),
        'Thoracic': GraphLine(graphLineColors[lineColorIndex++], 'Thoracic',
            (final PatientEvaluationGraphData eval) => eval.thoracic),
        'Trunk': GraphLine(graphLineColors[lineColorIndex++], 'Trunk',
            (final PatientEvaluationGraphData eval) => eval.trunk),
        'Trunk Inclination': GraphLine(
            graphLineColors[lineColorIndex++],
            'Trunk Inclination',
            (final PatientEvaluationGraphData eval) => eval.trunkInclincation),
        'Elbow Extension': GraphLine(
            graphLineColors[lineColorIndex++],
            'Elbow Extension',
            (final PatientEvaluationGraphData eval) => eval.elbowExtension),
      };

      for (final ChoiceData<String> posture in multipleSelectedPostures) {
        if (postureMap.containsKey(posture.value)) {
          final GraphLine line = postureMap[posture.value]!;
          addGraphLine(line.color, line.label, line.patientEvalFunction);
        }
      }
    });
  }

  /// Add a graph line with the provided color, label and mapper function
  /// E.g (Colors.red, 'Trunk', (PatientEvaluationGraphData eval) => eval.trunk))
  void addGraphLine(final Color lineColor, final String label,
      final Function(PatientEvaluationGraphData eval) yValueMapperFn) {
    graphLines.add(LineSeries<PatientEvaluationGraphData, String>(
      key: ValueKey<String>('${graphLines.length}'),
      // Key is VERY IMPORTANT - will
      color: lineColor,
      name: label,
      dataSource: filteredGraphData,
      xValueMapper: (final PatientEvaluationGraphData patientEval, final _) =>
          '${patientEval.dateTaken.year}/${patientEval.dateTaken.month}/${patientEval.dateTaken.day}',
      yValueMapper: (final PatientEvaluationGraphData patientEval, final _) =>
          yValueMapperFn(patientEval),
    ));
  }

  /// Toggles the averages lines on the graph
  void toggleShowAveragesSwitch() {
    if (mounted) {
      setState(() {
        switchState = !switchState;
        updateGraphLines();
      });
    }
  }

  void filterGraphByDates(final DateRange selectedDateRange) {
    // Clear data
    filteredGraphData.clear();

    // Filter the data inclusive for start and end
    for (final PatientEvaluationGraphData data in graphData) {
      if (!data.dateTaken.isBefore(selectedDateRange.start) &&
          !data.dateTaken.isAfter(selectedDateRange.end)) {
        if (data.exclude) {
          excludedEvalsCount++;
          continue;
        }
        filteredGraphData.add(data);
      }
    }

    updateFoundEvaluationStatus(filteredGraphData.length);
    updateGraphLines();
  }

  void updateFoundEvaluationStatus(final int count) {
    // Do proper grammar and use singular or plural
    final bool isSingular = count == 1;
    final String evaluations = isSingular ? 'evaluation' : 'evaluations';

    foundEvaluationStatusMessage =
        'Found $count $evaluations, $excludedEvalsCount excluded.';

    if (mounted) {
      setState(() {});
    }
  }

  /// Method to build the top section of the page with two columns
  Widget _buildTopSection() {
    return Expanded(
      flex: 2, // Adjust the height of the top section
      child: Row(
        children: [_buildTopColumnOne(), _buildTopColumnTwo()],
      ),
    );
  }

  /// Method to build the bottom graph section
  Widget _buildGraph() {
    return Expanded(
      flex: 6,
      child: filteredGraphData.length > 1 && graphLines.isNotEmpty
          ? SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: SizedBox(
                width: 650,
                height: MediaQuery.of(context).size.height * 0.5,
                child: Transform.rotate(
                  angle: -90 * 3.14 / 180,
                  child: SfCartesianChart(
                      legend: Legend(
                        isVisible: true,
                        position: LegendPosition.auto,
                        legendItemBuilder: (String name, dynamic series,
                            dynamic point, int index) {
                          return RotatedBox(
                            quarterTurns: 1, // 90 degrees
                            child: Row(children: [
                              Container(
                                width: 16,
                                height: 16,
                                margin: const EdgeInsets.only(right: 10),
                                decoration: BoxDecoration(
                                  color: series.color,
                                  shape: BoxShape.circle,
                                ),
                              ),
                              Text(
                                name,
                                style: const TextStyle(fontSize: 12),
                              ),
                            ]),
                          );
                        },
                      ),
                      // Later might be worth it to make a custom legend
                      primaryXAxis: const CategoryAxis(
                        labelRotation: 90,
                      ),
                      primaryYAxis: const NumericAxis(
                        labelRotation: 90,
                      ),
                      series: graphLines),
                ),
              ),
            )
          : filteredGraphData.isNotEmpty && graphLines.isEmpty
              ? const IconMessageWidget(
                  message: "No lines selected", icon: Icons.bar_chart)
              : const IconMessageWidget(
                  message: "Not enough data for graph", icon: Icons.bar_chart),
    );
  }

  /// First top column with date range picker and count of found evaluations
  Widget _buildTopColumnOne() {
    return Expanded(
        child: Column(
      mainAxisAlignment: MainAxisAlignment.center,
      spacing: 12,
      children: [
        DateRangeField(
          key: dateRangeKey,
          enabled: isDateRangeButtonEnabled,
          decoration: InputDecoration(
            // If date range is disabled change label
            label: isDateRangeButtonEnabled
                ? const Text("Date range picker")
                : const Text("Date range picker [No session data: disabled]"),
            hintText: 'Please select a date range',
          ),
          onDateRangeSelected: (final DateRange? value) {
            if (mounted) {
              // Make sure a full date range has been selected
              if (value != null) {
                // filter the graph data with the new range
                filterGraphByDates(value);
                // Update the selected range
                setState(() {
                  selectedDateRange = value;
                });
              }
            }
          },
          selectedDateRange: selectedDateRange,
          pickerBuilder: datePickerBuilder,
        ),
        Text(foundEvaluationStatusMessage,
            style: TextStyle(
                color: filteredGraphData.isEmpty ? Colors.red : Colors.black,
                fontWeight: FontWeight.bold,
                fontSize: 17)),
      ],
    ));
  }

  /// A widget to display the date picker popup dialog
  Widget datePickerBuilder(final BuildContext context,
          final dynamic Function(DateRange?) onDateRangeChanged,
          [final bool doubleMonth = true]) =>
      DateRangePickerWidget(
        doubleMonth: doubleMonth,
        quickDateRanges: [
          QuickDateRange(
            label: 'All Evaluations',
            dateRange: initialDateRange,
          ),
          QuickDateRange(
            label: '30 days before last session',
            dateRange: DateRange(
              initialDateRange.end.subtract(const Duration(days: 30)),
              initialDateRange.end,
            ),
          ),
          QuickDateRange(
            label: '90 days before last session',
            dateRange: DateRange(
              initialDateRange.end.subtract(const Duration(days: 90)),
              initialDateRange.end,
            ),
          ),
          QuickDateRange(
            label: 'Last year before last session',
            dateRange: DateRange(
              initialDateRange.end.subtract(const Duration(days: 365)),
              initialDateRange.end,
            ),
          ),
        ],
        minimumDateRangeLength: 2,
        initialDateRange: selectedDateRange,
        initialDisplayedDate: initialDateRange.start,
        onDateRangeChanged: onDateRangeChanged,
        height: 350,
        theme: const CalendarTheme(
          selectedColor: Colors.blue,
          dayNameTextStyle: TextStyle(color: Colors.black45, fontSize: 10),
          inRangeColor: Color(0xFFD9EDFA),
          inRangeTextStyle: TextStyle(color: Colors.blue),
          selectedTextStyle: TextStyle(color: Colors.white),
          todayTextStyle: TextStyle(fontWeight: FontWeight.bold),
          defaultTextStyle: TextStyle(color: Colors.black, fontSize: 12),
          radius: 10,
          tileSize: 40,
          disabledTextStyle: TextStyle(color: Colors.grey),
          quickDateRangeBackgroundColor: Color(0xFFFFF9F9),
          selectedQuickDateRangeColor: Colors.blue,
        ),
      );

  /// The right top column with a toggle button for averages
  /// and the select postures button
  Widget _buildTopColumnTwo() {
    return Expanded(
        child: Column(
      children: [
        Padding(
            padding: const EdgeInsets.symmetric(vertical: 15),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                GestureDetector(
                  // Make the text clickable
                  onTap: () {
                    if (mounted) {
                      // toggle the switch.
                      setState(() {
                        toggleShowAveragesSwitch();
                      });
                    }
                  },
                  child: const Text(
                    'Posture Averages', // Text label for the switch
                    style: TextStyle(
                      fontSize: 17, // Font size
                      fontWeight: FontWeight.bold, // Font weight
                    ),
                  ),
                ),
                Switch(
                  // This bool value toggles the switch.
                  value: switchState,
                  activeColor: Colors.red,
                  onChanged: (final bool value) {
                    if (mounted) {
                      // This is called when the user toggles the switch.
                      setState(() {
                        toggleShowAveragesSwitch();
                      });
                    }
                  },
                ),
              ],
            )),
        postureSelectButton
      ],
    ));
  }

  void setChoicesValue(final List<ChoiceData<String>> value) {
    if (mounted) {
      setState(() => multipleSelectedPostures = value);
      updateGraphLines();
    }
  }

  void setupPosturesButton() {
    postureSelectButton = FutureBuilder<List<ChoiceData<String>>>(
        initialData: posturesData,
        future: null,
        builder: (final context, final snapshot) {
          return SizedBox(
            width: 300,
            child: Card(
              child: PromptedChoice<ChoiceData<String>>.multiple(
                title: 'Select Postures',
                clearable: true,
                error: snapshot.hasError,
                errorBuilder: ChoiceListError.create(
                  message: snapshot.error.toString(),
                ),
                loading: snapshot.connectionState == ConnectionState.waiting,
                value: defaultSelectedPostures,
                onChanged: setChoicesValue,
                itemCount: snapshot.data?.length ?? 0,
                itemBuilder: (final state, final i) {
                  final choice = snapshot.data?.elementAt(i);
                  return CheckboxListTile(
                    value: state.selected(choice!),
                    onChanged: state.onSelected(choice),
                    title: Text(choice.title),
                    subtitle: choice.subtitle != null
                        ? Text(
                            choice.subtitle!,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          )
                        : null,
                    secondary: choice.image != null
                        ? CircleAvatar(
                            backgroundImage: NetworkImage(choice.image!),
                          )
                        : null,
                  );
                },
                modalHeaderBuilder: ChoiceModal.createHeader(
                  title: const Text('Select Postures'),
                  actionsBuilder: [
                    (final state) {
                      final values = snapshot.data!;
                      return Checkbox(
                        semanticLabel: 'Select/Deselect All',
                        value: state.selectedMany(values),
                        onChanged: state.onSelectedMany(values),
                        tristate: true,
                      );
                    },
                    ChoiceModal.createSpacer(width: 25),
                  ],
                ),
                promptDelegate: ChoicePrompt.delegateBottomSheet(),
                anchorBuilder: ChoiceAnchor.create(valueTruncate: 1),
              ),
            ),
          );
        });
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [_buildTopSection(), _buildGraph()],
        ),
      ),
    );
  }
}
