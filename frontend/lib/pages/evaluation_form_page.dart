import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/local_controllers/local_evaluation_controller.dart';
import 'package:frontend/controllers/patient_evaluation_controller.dart';
import 'package:frontend/models/evaluation.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/pose.dart';
import 'package:frontend/pages/completed_evaluation_page.dart';
import 'package:frontend/pages/patient_info_page.dart';
import 'package:frontend/widgets/helper_widgets/alert_dialog_box.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/evaluation_form_widgets/question_widget.dart';
import 'package:frontend/widgets/session_evaluation_view_widgets/notes_widgets/notes_widget.dart';
import 'package:localstore/localstore.dart';

class EvaluationForm extends StatefulWidget {
  static const String EVAL_TYPE = "evalType";
  static const String SESSION_ID = "sessionID";
  static const String FORM_DATA = "formData";
  static const String SELECTED_IMAGES = "selectedImages";

  const EvaluationForm(
      {required this.patient,
      required this.sessionID,
      required this.evalType,
      required this.data,
      super.key});

  final Patient patient;
  final String sessionID;
  final String evalType;
  final Map<String, dynamic>? data;

  @override
  State<EvaluationForm> createState() => _EvaluationFormState();
}

class _EvaluationFormState extends State<EvaluationForm>
    with SingleTickerProviderStateMixin {
  final _formKey = GlobalKey<FormBuilderState>();
  final _db = Localstore.getInstance();
  late List<String> uniqueTabs; // List of unique tabs
  late TabController _tabController;
  Map<String, dynamic> _initialValues = {};
  final AuthController _authController = AuthController();
  List<String> validTags = [];

  // dictionaries to update and keep states
  Map<String, String?> selectedImages = {};

  /// attempts to save the evaluation to the backend silently,
  /// will not throw an error, but will issue a onetime reminder that
  /// the user is offline, and that caching will be done locally
  Future<void> attemptToCacheEvaluation() async {
    final Map<String, dynamic> mapFormFieldData = {};
    mapFormFieldData[EvaluationForm.FORM_DATA] = _formKey.currentState!.value;
    mapFormFieldData[EvaluationForm.SESSION_ID] = widget.sessionID;
    mapFormFieldData[EvaluationForm.EVAL_TYPE] = widget.evalType;
    mapFormFieldData[EvaluationForm.SELECTED_IMAGES] = selectedImages;

    // tack into the JSON the type of evaluation that this is
    try {
      await PatientEvaluationController.cacheEval(
          widget.patient.id!, mapFormFieldData, widget.sessionID);
    } catch (e) {
      debugPrint(e.toString());
    }
  }

  void updateQuestionState(final String category, final String? imagePath) {
    setState(() {
      selectedImages[category] = imagePath;
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  void initState() {
    super.initState();
    uniqueTabs = grabUniqueTabs();
    _tabController = TabController(length: uniqueTabs.length, vsync: this);

    // add a listener to the TabController to rebuild when the tab changes
    _tabController.addListener(() {
      setState(() {}); // trigger a rebuild to update the stack
    });

    WidgetsBinding.instance.addPostFrameCallback((final _) async {
      // load saved data first
      loadSavedFormData();
    });
    initTagList();
  }

  // Method to grab unique tabs from PoseGroup
  List<String> grabUniqueTabs() {
    final List<String> uniqueTabs = [];
    for (final pose in PoseGroup.values) {
      if (!uniqueTabs.contains(pose.tab)) {
        uniqueTabs.add(pose.tab);
      }
    }
    uniqueTabs.add("Notes");
    return uniqueTabs;
  }

  // Create tabs based on unique tabs
  List<Widget> createTabs() {
    return uniqueTabs
        .map((final tabGroup) => Tab(
              text: tabGroup,
              key: ValueKey("${tabGroup}Tab"),
            ))
        .toList();
  }

  void initTagList() async {
    validTags = await PatientEvaluationController.getEvaluationTagList();
  }

  void loadSavedFormData() async {
    debugPrint("Loading saved form data...");

    final data = widget.data;

    if (data != null && mounted) {
      Map<String, dynamic> formData = {};

      if (data[EvaluationForm.FORM_DATA] != null) {
        formData = Map<String, dynamic>.from(data[EvaluationForm.FORM_DATA]);
        // debugPrint("Loaded form data: $formData");
      }

      setState(() {
        _initialValues = formData;
        selectedImages = Map<String, String?>.from(
            data[EvaluationForm.SELECTED_IMAGES] ?? {});
      });

      // cycle through all tabs to ensure form fields are created
      for (int i = 0; i < uniqueTabs.length; i++) {
        _tabController.animateTo(i);
      }

      // return to first tab
      if (mounted) {
        setState(() {
          _tabController.index = 0;
          _formKey.currentState?.patchValue(_initialValues);
        });
      }
    }
  }

  // method to handle the submission of the form
  void handleSubmission(final FormBuilderState? formInfo) async {
    if (_formKey.currentState?.saveAndValidate() ?? false) {
      try {
        // creating evaluation
        debugPrint(widget.sessionID);
        final PatientEvaluation pEval = PatientEvaluation(
            widget.evalType,
            widget.sessionID,
            '',
            // evaluationID will be set by backend
            false,
            formInfo?.value['notes'],
            formInfo?.value['hip_flex'],
            formInfo?.value['lumbar'],
            formInfo?.value['head_ant'],
            formInfo?.value['head'],
            formInfo?.value['knee_flex'],
            formInfo?.value['pelvic'],
            formInfo?.value['pelvic_tilt'],
            formInfo?.value['thoracic'],
            formInfo?.value['trunk'],
            formInfo?.value['trunk_inclination'],
            formInfo?.value['elbow_extension']);

        final List<String> evaluationTags =
            await PatientEvaluationController.getEvaluationsTagsFromNotes(
                pEval.notes ?? "");

        if (evaluationTags.isNotEmpty) {
          // The user has entered at least one tag, ask them if they are sure they want to submit the evaluation
          // If they answer yes, continue.
          // If they answer no, exit the function, without submitting. (effectively cancels the submission)
          bool submitEvalWithTags = true;
          if (mounted) {
            submitEvalWithTags = await showDialog<bool>(
                    context: context,
                    builder: (final BuildContext context) {
                      return AlertDialog(
                          title: const Text('Evaluation will be excluded'),
                          content: Text(
                              'Evaluation will be excluded from graph analysis if submitted.\n'
                              'Tags excluding this evaluation from analysis: $evaluationTags\n'
                              'Do you want to submit anyways?'),
                          actions: <Widget>[
                            TextButton(
                                onPressed: () {
                                  Navigator.of(context).pop(false);
                                },
                                child: const Text("No")),
                            TextButton(
                                key: const Key("submitEvaluationYesTags"),
                                onPressed: () {
                                  Navigator.of(context).pop(true);
                                },
                                child: const Text("Yes"))
                          ]);
                    }) ??
                true;
          }
          if (!submitEvalWithTags) {
            return;
          }
        }

        if (_authController.isGuestLoggedIn()) {
          await LocalEvaluationController.createEvaluation(pEval);
        }
        else {
          await PatientEvaluationController.postEvaluationToDatabase(
              pEval, widget.patient.id!);
        }



        // also delete the cached evaluation once submitted successfully
        // (This is a try/catch) so if submitting it failed, there will be no deletion
        final AuthController authController = AuthController();
        if (!authController.isGuestLoggedIn()) {
          await PatientEvaluationController.clearCachedEval(
              widget.patient.id!, widget.sessionID, widget.evalType);
        }
        

        bool viewCompletedEval = false;
        // if success (try will catch the error and skip this section)
        if (mounted) {
          viewCompletedEval = await showDialog<bool>(
                context: context,
                builder: (final BuildContext context) {
                  return AlertDialog(
                    title: const Text('Evaluation Submission Received'),
                    content: Text(
                      'Evaluation successfully submitted for '
                      '${widget.patient.fName} ${widget.patient.lName}.\n'
                      'Would you like to view it?',
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
                        key: const Key("submitEvaluationYes"),
                        onPressed: () {
                          Navigator.of(context).pop(true); // user confirms
                        },
                        child: const Text('Yes'),
                      ),
                    ],
                  );
                },
              ) ??
              false;
        }

        // pop 2 off the router stack so it goes back to session view
        // and then take the user to see the evaluation
        if (mounted && viewCompletedEval) {
          Navigator.pop(context);
          Navigator.pop(context);

          alertDialogBox(context, 'Form submitted successfully');

          _db
              .collection('evaluations')
              .doc('${widget.sessionID}_${widget.evalType}')
              .delete(); // delete the document so session-view correctly displays completed instead of progress

          mounted
              ? Navigator.pushReplacement(
                  context,
                  MaterialPageRoute(
                      builder: (final context) => CompletedEvaluationView(
                          eval: pEval,
                          title:
                              '${widget.patient.fName} ${widget.patient.lName}\'s '
                              '${(widget.evalType)[0].toUpperCase()}'
                              '${widget.evalType.substring(1)}-Assessment Evaluation')))
              : null;
        } else if (mounted) {
          Navigator.pushReplacement(
              context,
              MaterialPageRoute(
                  builder: (final context) =>
                      PatientInfoPage(patient: widget.patient)));
        } else {
          debugPrint('Error occurred async');
        }
      } catch (e) {
        // show error message
        if (mounted) {
          debugPrint(e.toString());
          alertDialogBox(context, 'Error submitting form: $e');
        }
      }
    } else {
      alertDialogBox(context, 'Please complete entire form');
    }
  }

  @override
  Widget build(final BuildContext context) {
    return DefaultTabController(
      length: uniqueTabs.length,
      child: FormBuilder(
        key: _formKey,
        initialValue: _initialValues,
        onChanged: () async {
          _formKey.currentState?.save();

          if (_formKey.currentState?.value != null) {
            final Map<String, dynamic> formData =
                Map<String, dynamic>.from(_formKey.currentState!.value);
            await _db
                .collection('evaluations')
                .doc('${widget.sessionID}_${widget.evalType}')
                .set({
              EvaluationForm.FORM_DATA: formData,
              EvaluationForm.SELECTED_IMAGES: selectedImages,
            });
          }
          // If not logged in as guest cache eval
          if (!AuthController().isGuestLoggedIn()) {
            await attemptToCacheEvaluation();
          }
        },
        child: Scaffold(
          appBar: AppBar(
            backgroundColor: Theme.of(context).colorScheme.primary,
            actions: _formKey.currentState?.saveAndValidate() ?? false
                ? [
                    ElevatedButton(
                      onPressed: () => handleSubmission(_formKey.currentState),
                      child: const Text("Submit"),
                    )
                  ]
                : null,
            bottom: TabBar(
              controller: _tabController,
              tabs: createTabs(),
            ),
          ),
          body: IndexedStack(
            index: _tabController.index, // sync stack with controller
            children: uniqueTabs.asMap().entries.map((final entry) {
              final String tab = entry.value;
              return SingleChildScrollView(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: createQuestions(tab),
                ),
              );
            }).toList(),
          ),
        ),
      ),
    );
  }

// create questions for the options
  List<Widget> createQuestions(final String tab) {
    if (tab == "Notes") {
      return [
        NotesWidget(
          formKey: _formKey,
          validTags: validTags,
        )
      ];
    }

    final List<Widget> lstQuestions = [];

    // group questions by category
    final Map<String, List<FormBuilderChipOption>> optionsByCategory = {};

    // first pass - group all options by their category
    for (final pose in PoseType.values) {
      if (pose.category.tab == tab) {
        final String category = pose.dbCategoryKey;
        if (!optionsByCategory.containsKey(category)) {
          optionsByCategory[category] = [];
        }

        optionsByCategory[category]!.add(FormBuilderChipOption(
          key: ValueKey(pose.toString()),
          value: pose.value,
          child: SizedBox(
            width: 50,
            child: Image.asset(pose.imgPath),
          ),
        ));
      }
    }

    // second pass - create questions for each category
    optionsByCategory.forEach((final category, final options) {
      lstQuestions.add(QuestionWidget(
        formKey: _formKey,
        category: category,
        options: options,
        initialImage: selectedImages[category],
        onStateChanged: updateQuestionState,
      ));
    });

    return lstQuestions;
  }
}
