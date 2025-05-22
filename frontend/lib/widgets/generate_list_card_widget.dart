import 'package:azlistview/azlistview.dart';
import 'package:emoji_picker_flutter/emoji_picker_flutter.dart';
import 'package:flutter/material.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/therapist.dart';
import 'package:frontend/pages/therapist_list_page.dart';
import 'package:frontend/pages/tutorial_page.dart';
import 'package:frontend/widgets/helper_widgets/emojis/custom_emoji_view.dart';
import 'package:tutorial_coach_mark/tutorial_coach_mark.dart';

class GenerateListCardWidget extends StatefulWidget {
  final List<ISuspensionBean> objects;
  final bool roleCheck;
  final Function(ISuspensionBean object)? deleteClick;
  final Function(ISuspensionBean object)? cardClick;
  final Function(ISuspensionBean object)? editClick;
  final bool patientMoveBar;
  final String? title;
  final Therapist? therapistOld;
  final GlobalKey? editButtonKey;
  final GlobalKey? archiveButtonKey;
  final GlobalKey? toggleKey;
  final GlobalKey? patientCheckboxKey;
  final GlobalKey? reassignToButtonKey;
  final bool showTutorial;

  const GenerateListCardWidget({
    super.key,
    required this.objects,
    required this.roleCheck,
    required this.patientMoveBar,
    this.deleteClick,
    this.cardClick,
    this.editClick,
    this.title,
    this.therapistOld,
    this.editButtonKey,
    this.archiveButtonKey,
    this.toggleKey,
    this.patientCheckboxKey,
    this.reassignToButtonKey,
    this.showTutorial = false,
  });

  @override
  State<GenerateListCardWidget> createState() => _GenerateListCardWidgetState();
}

class _GenerateListCardWidgetState extends State<GenerateListCardWidget> {
  List<Patient> patients = [];
  List<Therapist> therapists = [];
  late String name;
  late Type roleType;
  late Therapist? therapistOld;

  List<Patient> selectedPatients = [];
  bool assignMode = false; // Toggle for selectable state
  final GlobalKey _toggleKey = GlobalKey();
  final GlobalKey _patientCheckboxKey = GlobalKey();
  final GlobalKey _reassignToButtonKey = GlobalKey();

  // Function to toggle assign mode
  void toggleAssignMode() {
    setState(() {
      assignMode = !assignMode;
      if (!assignMode) {
        selectedPatients.clear(); // Clear selection when exiting assign mode
      }
    });
  }

  @override
  void initState() {
    super.initState();
    roleType = widget.roleCheck ? Therapist : Patient;
    name = widget.roleCheck ? "therapist" : "patient";
    initList(widget.objects);
    therapistOld = widget.therapistOld;
    if (widget.showTutorial && widget.patientMoveBar) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        showReassignmentTutorial();
      });
    }
  }

  // Init list and order it.
  void initList(List<ISuspensionBean> objectsFromPage) {
    if (widget.objects.isNotEmpty) {
      if (widget.roleCheck) {
        therapists.addAll(widget.objects.whereType<Therapist>().toList());
      } else {
        patients.addAll(widget.objects.whereType<Patient>().toList());
      }

      SuspensionUtil.sortListBySuspensionTag(widget.objects);
      SuspensionUtil.setShowSuspensionStatus(widget.objects);
    }
    if (mounted) {
      setState(() {});
    }
  }

  // show the emojipicker within a dialog so on screen
// also takes a patient to send an edit to submit emoji selection to db
  Future<void> showEmojiPicker(final TextEditingController controller,
      final Patient patient) {
    return showDialog(
      context: context,
      builder: (final BuildContext context) {
        return Dialog(
          // 3rd party emoji picker with lots of local modifications in widget/helper_widgets/emojis
          child: EmojiPicker(
            // text controller will store the selected emoji inside of itself
            textEditingController: controller,
            // scroll controller will store scroll functions of the emoji view
            scrollController: ScrollController(),
            // our custom widget
            customWidget: (final config, final state, final showSearchBar) =>
                CustomView(config, state, showSearchBar, controller, patient),
            // overall config of the emoji picker
            config: Config(
              height: MediaQuery
                  .of(context)
                  .size
                  .height * 0.4,
              checkPlatformCompatibility: true,
              emojiViewConfig: const EmojiViewConfig(
                backgroundColor: Colors.white,
              ),
              // handles the category view, top bar of the emoji picker
              categoryViewConfig: CategoryViewConfig(
                iconColorSelected: Theme
                    .of(context)
                    .highlightColor,
                indicatorColor: Colors.white,
                backgroundColor: Theme
                    .of(context)
                    .colorScheme
                    .primary,
                iconColor: Colors.black,
                dividerColor: Colors.black,
              ),
              // handles config of search bar, bottom bar of the emoji picker
              bottomActionBarConfig: BottomActionBarConfig(
                backgroundColor: Theme
                    .of(context)
                    .colorScheme
                    .primary,
                buttonColor: Theme
                    .of(context)
                    .highlightColor,
                buttonIconColor: Theme
                    .of(context)
                    .primaryColor,
              ),
            ),
          ),
        );
      },
    );
  }

  // Show Reassignment Tutorial
  void showReassignmentTutorial() {
    setState(() {
      assignMode = false;
      selectedPatients.clear();
    });

    final targets = [
      TargetFocus(
        identify: "reassign_toggle",
        keyTarget: widget.toggleKey ?? _toggleKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "To assign patients to a different therapist you must first be"
                  " logged in as an owner. Once logged in as an owner, navigate"
                  " to the Therapists page and select a therapist with patients"
                  " under their care.\n\nOnce here, click the Reassign Patients"
                  " button to toggle patient reassignment mode.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
    ];

    TutorialCoachMark(
      targets: targets,
      colorShadow: Colors.black54,
      onFinish: () {
        setState(() {
          assignMode = true;
        });
        _showPatientCheckboxTutorial();
      },
      onSkip: () {
        debugPrint("Reassignment tutorial skipped");
        setState(() {
          assignMode = false;
          selectedPatients.clear();
        });
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
        return true;
      },
    ).show(context: context);
  }

  // Second step: Highlight the patient checkbox
  void _showPatientCheckboxTutorial() {
    final targets = [
      TargetFocus(
        identify: "patient_checkbox",
        keyTarget: widget.patientCheckboxKey ?? _patientCheckboxKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.top,
            child: const Text(
              "Next, select the checkbox next to the patient(s) you wish "
                  "to reassign.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
    ];

    TutorialCoachMark(
      targets: targets,
      colorShadow: Colors.black54,
      onFinish: () {
        if (patients.isNotEmpty) {
          setState(() {
            selectedPatients.add(patients[0]);
          });
        }
        _showReassignToButtonTutorial();
      },
      onSkip: () {
        debugPrint("Reassignment tutorial skipped");
        setState(() {
          assignMode = false;
          selectedPatients.clear();
        });
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
        return true;
      },
    ).show(context: context);
  }

  // Third step: Highlight the Reassign to button
  void _showReassignToButtonTutorial() {
    final targets = [
      TargetFocus(
        identify: "reassign_to_button",
        keyTarget: widget.reassignToButtonKey ?? _reassignToButtonKey,
        enableOverlayTab: true,
        contents: [
          TargetContent(
            align: ContentAlign.bottom,
            child: const Text(
              "Finally, tap the 'Reassign to...' button and select the therapist"
                  " you would like the patient to be assigned to. If successful,"
                  " the patient will now be assigned to the selected therapist.",
              style: TextStyle(color: Colors.white, fontSize: 20),
            ),
          ),
        ],
      ),
    ];

    TutorialCoachMark(
      targets: targets,
      colorShadow: Colors.black54,
      onFinish: () {
        debugPrint("Reassignment tutorial finished");
        setState(() {
          assignMode = false;
          selectedPatients.clear();
        });
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
      },
      onSkip: () {
        debugPrint("Reassignment tutorial skipped");
        setState(() {
          assignMode = false;
          selectedPatients.clear();
        });
        Navigator.pushReplacementNamed(context, TutorialPage.RouteName);
        return true;
      },
    ).show(context: context);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: widget.patientMoveBar
          ? AppBar(
        backgroundColor: Theme
            .of(context)
            .colorScheme
            .primary,
        title: Row(
          children: [
            Expanded(
              child: Text(widget.title ?? 'Patients'),
            ),
            Tooltip(
              message: 'Toggle patient reassignment mode',
              child: TextButton(
                key: widget.toggleKey ?? _toggleKey,
                onPressed: toggleAssignMode,
                style: TextButton.styleFrom(
                  backgroundColor: assignMode ? Colors.green : Colors.red,
                  foregroundColor: Colors.black,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                child: Text(
                  'Reassign Patients: ${assignMode ? 'on' : 'off'}',
                ),
              ),
            ),
            const Spacer(flex: 1),
          ],
        ),
        actions: [
          Visibility(
            visible: assignMode && selectedPatients.isNotEmpty,
            maintainSize: true,
            maintainAnimation: true,
            maintainState: true,
            child: Tooltip(
              message: 'Choose a new therapist to reassign patients',
              child: TextButton(
                key: widget.reassignToButtonKey ?? _reassignToButtonKey,
                onPressed: () {
                  if (mounted && therapistOld != null) {
                    Navigator.pushReplacement(
                      context,
                      MaterialPageRoute(
                        builder: (context) =>
                            TherapistListPage(
                              therapistThatWillHavePatientsMoved: therapistOld!,
                              therapistAssignmentTitle:
                              'Reassign ${selectedPatients
                                  .length} patient${selectedPatients.length == 1
                                  ? ''
                                  : 's'} '
                                  'from ${therapistOld!.fName} to ...',
                              lstPatientsReassignment: selectedPatients,
                            ),
                      ),
                    );
                  }
                },
                style: TextButton.styleFrom(
                  backgroundColor: Colors.green,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                child: const Text(
                  'Reassign to...',
                  style: TextStyle(color: Colors.black),
                ),
              ),
            ),
          ),
        ],
      )
          : null,
      body: AzListView(
        key: Key('${name}_list_widget'),
        data: widget.objects,
        itemCount: widget.objects.length,
        itemBuilder: (context, index) {
          final identity = widget.objects[index];
          Color? cardColor;

          final fullName = widget.roleCheck
              ? "${(identity as Therapist).fName} ${(identity as Therapist)
              .lName}"
              : "${(identity as Patient).fName} ${(identity as Patient).lName}";

          if (index == 0 ||
              widget.objects[index].getSuspensionTag() !=
                  widget.objects[index - 1].getSuspensionTag()) {
            cardColor = Colors.grey[300];
          } else {
            cardColor = index % 2 == 0 ? Colors.grey[300] : Colors.blueGrey;
          }

          // assign the emojis to the patient
          final TextEditingController _emojiController = TextEditingController();
          if (widget.roleCheck == false) {
            if ((identity as Patient).emoji == null ||
                (identity as Patient).emoji!.isEmpty) {
              // if it is null or empty, assign warning as default emoji
              _emojiController.text = "⚠️";
            } else {
              _emojiController.text = (identity as Patient).emoji!;
            }
          }

          return Card(
            margin: const EdgeInsets.symmetric(horizontal: 5, vertical: 10),
            key: Key('$name-card-$index'),
            color: cardColor,
            child: ListTile(
              title: Row(
                children: [
                  // display emojis here
                  if (widget.roleCheck == false)
                    MaterialButton(
                      // allow for emoji button to be pressable
                      key: Key(
                          "${(identity as Patient).fName
                              .toLowerCase()}${(identity as Patient).lName
                              .toLowerCase()}-emoji-$index"),
                      onPressed: () =>
                          showEmojiPicker(
                              _emojiController, identity as Patient),
                      child: ValueListenableBuilder(
                        // will reload display when value changes which is text controller
                        valueListenable: _emojiController,
                        builder: (context, text, child) {
                          return RichText(
                            text: TextSpan(
                              // dynamically size the button and the emoji based on screen size
                              style: TextStyle(
                                  fontSize:
                                  MediaQuery
                                      .of(context)
                                      .textScaler
                                      .scale(40)),
                              text: _emojiController.text,
                            ),
                          );
                        },
                      ),
                    ),
                  FittedBox(child: Text(fullName)), // display the patient name
                ],
              ),
              onTap: () {
                if (widget.cardClick != null && !assignMode) {
                  widget.cardClick!(identity);
                } else if (assignMode && !widget.roleCheck) {
                  setState(() {
                    final patient = identity as Patient;
                    if (selectedPatients.contains(patient)) {
                      selectedPatients.remove(patient);
                    } else {
                      selectedPatients.add(patient);
                    }
                  });
                }
              },
              trailing: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (assignMode && !widget.roleCheck)
                    Checkbox(
                      key: index == 0
                          ? (widget.patientCheckboxKey ?? _patientCheckboxKey)
                          : null,
                      value: selectedPatients.contains(identity),
                      onChanged: (bool? value) {
                        setState(() {
                          final patient = identity as Patient;
                          if (value == true) {
                            selectedPatients.add(patient);
                          } else {
                            selectedPatients.remove(patient);
                          }
                        });
                      },
                    ),
                  if (widget.editClick != null && !assignMode)
                    Tooltip(
                      message: 'Edit $name',
                      child: IconButton(
                        key: index == 0 ? widget.editButtonKey : null,
                        icon: const Icon(Icons.edit),
                        onPressed: () => widget.editClick!(identity),
                      ),
                    ),
                  if (widget.deleteClick != null && !assignMode)
                    Tooltip(
                      message: 'Archive $name',
                      child: IconButton(
                        key: index == 0 ? widget.archiveButtonKey : null,
                        icon: const Icon(Icons.archive),
                        onPressed: () async {
                          final confirmed = await showDialog<bool>(
                            context: context,
                            builder: (context) =>
                                AlertDialog(
                                  title: const Text('Are you sure?'),
                                  content:
                                  Text(
                                      'Do you really want to archive $fullName?'),
                                  actions: [
                                    TextButton(
                                      onPressed: () =>
                                          Navigator.of(context).pop(false),
                                      child: const Text('No'),
                                    ),
                                    TextButton(
                                      onPressed: () =>
                                          Navigator.of(context).pop(true),
                                      child: const Text('Yes'),
                                    ),
                                  ],
                                ),
                          );
                          if (confirmed == true && widget.deleteClick != null) {
                            widget.deleteClick!(identity);
                            setState(() {
                              widget.objects.removeAt(index);
                              if (widget.roleCheck) {
                                therapists.removeAt(index);
                              } else {
                                patients.removeAt(index);
                              }
                            });
                          }
                        },
                      ),
                    ),
                ],
              ),
            ),
          );
        },
        susItemBuilder: (context, index) {
          final tag = widget.objects[index].getSuspensionTag();
          return Container(
            height: 40,
            padding: const EdgeInsets.only(left: 16.0),
            color: Colors.grey,
            alignment: Alignment.centerLeft,
            child: Text(
              tag,
              style: const TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.bold,
              ),
            ),
          );
        },
      ),
    );
  }
}