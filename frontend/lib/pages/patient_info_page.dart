import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:dynamic_tabbar/dynamic_tabbar.dart';
import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_session_controller.dart';
import 'package:frontend/controllers/patient_evaluation_controller.dart';
import 'package:frontend/controllers/patient_session_controller.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/patient_evaluation_graph_data.dart';
import 'package:frontend/models/session.dart';
import 'package:frontend/pages/patient_session_view.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:frontend/widgets/graphs/patient_info_graph_tab_widget.dart';
import 'package:frontend/widgets/helper_widgets/icon_message_widget.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/patient_info_session_tab_widget.dart';
import 'package:http/http.dart';
import 'package:intl/intl.dart';
import 'package:tutorial_coach_mark/tutorial_coach_mark.dart';

class PatientInfoPage extends StatefulWidget {
  static const String RouteName = '/patient-info';
  const PatientInfoPage({super.key, required this.patient, this.showTutorial = false});

  final Patient patient;
  final bool showTutorial;

  @override
  State<PatientInfoPage> createState() => _PatientInfoPage();
}

class _PatientInfoPage extends State<PatientInfoPage> {
  List<Session> sessions = [];
  List<PatientEvaluationGraphData> graphEvaluationData = [];
  List<TabData> tabs = [];
  final AuthController _authController = AuthController();
  final GlobalKey _addSessionButtonKey = GlobalKey();
  final GlobalKey _graphTabKey = GlobalKey();
  final GlobalKey _sessionTabKey = GlobalKey();
  TabController? _tabController;
  late TutorialCoachMark tutorialCoachMark;

  // Method which does tasks upon state creation
  @override
  void initState() {
    super.initState();
    initTabs();
    initList();
  }

  Future<void> initList() async {
    await setSessions();
    // Don't get graph data in guest mode
    if (!_authController.isGuestLoggedIn()) {
      await setGraphData();
    }
    if (mounted && widget.showTutorial) {
      WidgetsBinding.instance.addPostFrameCallback((final _) {
        if (mounted) {
          try {
            showPatientSessionTutorial();
          } catch (e) {
            debugPrint("Failed to initialize tutorial: $e");
            if (mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Failed to start tutorial')),
              );
            }
          }
        }
      });
    }
  }

  void initTabs() {
    tabs = [
      TabData(
        index: 1,
        title: Tab(
          key: _sessionTabKey,
          child: const Tooltip(
            message: 'View past sessions',
            child: Text('Sessions'),
          ),
        ),
        content: const Center(child: Text('No sessions found')),
      ),
      TabData(
        index: 2,
        title: Tab(
          key: _graphTabKey,
          child: const Tooltip(
            message: 'View session data graphs',
            child: Text('Graph'),
          ),
        ),
        content: _authController.isGuestLoggedIn()
            ? const Center(child: Text('Please register an account to use this feature.'))
            : const Center(child: Text('Loading...')),
      ),
    ];
  }

  // set the sessions after init
  Future<void> setSessions() async {
    // Use mock data for tutorial
    if (widget.showTutorial) {
      sessions = [
        Session(
          patientID: widget.patient.id!,
          sessionID: 'mock_session_1',
          location: 'CA',
          dateTaken: DateTime.now().subtract(const Duration(days: 1)),
        ),
        Session(
          patientID: widget.patient.id!,
          sessionID: 'mock_session_2',
          location: 'CA',
          dateTaken: DateTime.now(),
        ),
      ];
    } else {
      // If in guest mode use local data
      if (_authController.isGuestLoggedIn()) {
        sessions = await LocalSessionController.getAllSessions(widget.patient.id);
      } else {
        sessions = await PatientSessionController.getAllSessions(widget.patient.id);
        debugPrint('Fetched sessions: ${sessions.map((final s) => s.dateTaken.toString()).toList()}');
      }
    }

    tabs[0] = TabData(
      index: 1,
      title: Tab(
        key: _sessionTabKey,
        child: const Tooltip(
          message: 'View past sessions',
          child: Text('Sessions'),
        ),
      ),
      content: sessions.isEmpty
          ? const IconMessageWidget(message: "No sessions for patient")
          : PatientInfoSessionTab(patient: widget.patient, sessions: sessions),
    );
    if (mounted) {
      setState(() {});
    }
  }

  // set graph data
  Future<void> setGraphData() async {
    // Use placeholder data for tutorial
    if (widget.showTutorial) {
      graphEvaluationData = [
        PatientEvaluationGraphData(
          dateTaken: DateTime.now(),
          sessionID: 'mock_session_1',
          evaluationID: 'mock_eval_1',
          evalType: 'mock_type',
          hipFlex: 5,
          lumbar: 5,
          headAnt: 5,
          headLat: 5,
          kneeFlex: 5,
          pelvic: 5,
          pelvicTilt: 5,
          thoracic: 5,
          trunk: 5,
          trunkInclincation: 5,
          elbowExtension: 5,
          exclude: false,
        ),
        PatientEvaluationGraphData(
          dateTaken: DateTime.now().subtract(const Duration(days: 1)),
          sessionID: 'mock_session_2',
          evaluationID: 'mock_eval_2',
          evalType: 'mock_type',
          hipFlex: 6,
          lumbar: 6,
          headAnt: 6,
          headLat: 6,
          kneeFlex: 6,
          pelvic: 6,
          pelvicTilt: 6,
          thoracic: 6,
          trunk: 6,
          trunkInclincation: 6,
          elbowExtension: 6,
          exclude: false,
        ),
      ];
    } else {
      graphEvaluationData = await PatientEvaluationController.getPatientEvaluations(widget.patient.id);
    }

    tabs[1] = TabData(
      index: 2,
      title: Tab(
        key: _graphTabKey,
        child: const Tooltip(
          message: 'View session data graphs',
          child: Text('Graph'),
        ),
      ),
      content: graphEvaluationData.isNotEmpty
          ? PatientInfoGraphTab(patientEvaluationGraphDataList: graphEvaluationData)
          : const PatientInfoGraphTab(patientEvaluationGraphDataList: []),
    );
    if (mounted) {
      setState(() {});
    }
  }

  // Tutorial for creating a new patient session
  void showPatientSessionTutorial() {
    final targets = [
      TargetFocus(
        identify: "add_new_session",
        keyTarget: _addSessionButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "To create a new patient session, from the patient list, "
                  "click on the patient’s name you want to create a new "
                  "session or evaluation for. You will be brought to the "
                  "Patient Info Page.\n\nFrom here, press the green + button "
                  "to begin a new patient session.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
      TargetFocus(
        identify: "view_session_tab",
        keyTarget: _sessionTabKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "With the session tab selected, you can view a patient's past "
                  "sessions, sorted by date.\n\n"
                  "Clicking on a session will allow you to see the results of "
                  "that particular session.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
      TargetFocus(
        identify: "view_graph_tab",
        keyTarget: _graphTabKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "From the graph tab, you can view a patient’s session data over "
                  "time. From here you can select a date range of sessions "
                  "to view. You can also decide whether to view data by "
                  "averages or not.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
    ];

    tutorialCoachMark = TutorialCoachMark(
      targets: targets,
      colorShadow: Colors.black54,
      onFinish: () {
        debugPrint("Patient session tutorial finished");
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
      },
      onSkip: () {
        debugPrint("Patient session tutorial skipped");
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
        return true;
      },
    );

    debugPrint("Starting patient session tutorial");
    tutorialCoachMark.show(context: context);
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Theme.of(context).colorScheme.primary,
        title: Text('${widget.patient.fName} ${widget.patient.lName}'),
        actions: [
          Material(
            color: Colors.green,
            shape: const CircleBorder(),
            child: IconButton(
              key: _addSessionButtonKey,
              icon: const Icon(Icons.add),
              onPressed: () async {
                final bool? isConfirmed = await showDialog<bool>(
                  context: context,
                  builder: (final BuildContext context) {
                    return AlertDialog(
                      title: Text('Creating a new session for:'
                          ' ${widget.patient.fName} ${widget.patient.lName}'),
                      content: Text(
                        'Are you sure you want to create a new session for '
                            '${widget.patient.fName} ${widget.patient.lName}?',
                      ),
                      actions: <Widget>[
                        TextButton(
                          onPressed: () {
                            // User cancels
                            Navigator.of(context).pop(false);
                          },
                          child: const Text('No'),
                        ),
                        TextButton(
                          onPressed: () {
                            Navigator.of(context).pop(true); // User confirms
                          },
                          child: const Text('Yes'),
                        ),
                      ],
                    );
                  },
                );

                // if the user wants to create a session, it will be done here
                if (isConfirmed ?? false) {
                  final nuPatientSession = Session(
                      patientID: widget.patient.id!,
                      sessionID: "",
                      // TODO, this should be from the therapists location
                      location: "CA",
                      dateTaken: DateTime.now());

                  // If in guest mode fetch from local storage
                  if (_authController.isGuestLoggedIn()) {
                    final bool success = await LocalSessionController.createSession(nuPatientSession);

                    final snackBarToShowUser = SnackBar(
                        duration: const Duration(seconds: 3),
                        content: success
                            ? const Text('Session created')
                            : const Text('Failed to create a session'));
                    // Check if the user has quickly left the page, (GUARD CONTEXT)
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(snackBarToShowUser);
                    }

                    // if this is successful, we push them to the session
                    if (success && context.mounted) {
                      Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (final context) => PatientSessionView(
                                  title:
                                  '${widget.patient.fName} ${widget.patient.lName}, ${DateFormat('yyyy-MM-dd').format(nuPatientSession.dateTaken)}, Evaluations',
                                  patient: widget.patient,
                                  session: nuPatientSession)));
                    }
                  }
                  // Connect to database
                  else {
                    final Response res = await PatientSessionController.createSession(nuPatientSession);

                    // Display to the user whether the session was created or not
                    final resResponses = jsonDecode(res.body);

                    // assign the session id after the response
                    if (res.statusCode == HttpStatus.ok) {
                      nuPatientSession.sessionID = resResponses['id'];
                    }

                    final snackBarToShowUser = SnackBar(
                        duration: const Duration(seconds: 3),
                        content: res.statusCode == HttpStatus.ok
                            ? Text('${resResponses['message']} Session ID is: ${resResponses['id']}')
                            : Text('Failed to create a session ${jsonDecode(res.body)}'));
                    // Check if the user has quickly left the page, (GUARD CONTEXT)
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(snackBarToShowUser);
                    }

                    // if this is successful, we push them to the session
                    if (res.statusCode == HttpStatus.ok && context.mounted) {
                      Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (final context) => PatientSessionView(
                                  title:
                                  '${widget.patient.fName} ${widget.patient.lName}, ${DateFormat('yyyy-MM-dd').format(nuPatientSession.dateTaken)}, Evaluations',
                                  patient: widget.patient,
                                  session: nuPatientSession)));
                    }
                  }
                }
              },
              tooltip: 'Add a new session for ${widget.patient.fName} ${widget.patient.lName}.',
              color: Colors.black,
            ),
          )
        ],
      ),
      body: Center(
        child: DynamicTabBarWidget(
          dynamicTabs: tabs,
          // set default tab index as per https://github.com/alihaider78222/dynamic_tabbar/issues/19
          onTabControllerUpdated: (final controller) {
            controller.index = 0;
          },
          // required to prevent errors
          onTabChanged: (final index) {},
        ),
      ),
    );
  }
}