import 'package:azlistview/azlistview.dart';
import 'package:emoji_picker_flutter/emoji_picker_flutter.dart';
import 'package:flutter/material.dart';
import 'package:frontend/main.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/therapist.dart';
import 'package:frontend/pages/therapist_list_page.dart';
import 'package:frontend/widgets/helper_widgets/emojis/custom_emoji_view.dart';

class GuestListCardWidget extends StatefulWidget {
  final List<ISuspensionBean> objects;
  final bool roleCheck;
  final Function(ISuspensionBean object)? deleteClick;
  final Function(ISuspensionBean object)? cardClick;
  final Function(ISuspensionBean object)? editClick;
  final bool patientMoveBar;
  final String? title;
  final Therapist? therapistOld;

  const GuestListCardWidget(
      {super.key,
      required this.objects,
      required this.roleCheck,
      required this.patientMoveBar,
      this.deleteClick,
      this.cardClick,
      this.editClick,
      this.title,
      this.therapistOld});

  @override
  State<GuestListCardWidget> createState() => _GuestListCardWidgetState();
}

class _GuestListCardWidgetState extends State<GuestListCardWidget> {
  List<Patient> patients = [];
  List<Therapist> therapists = [];
  late String name;
  late Type roleType;
  late Therapist? therapistOld;

  List<Patient> selectedPatients = [];
  bool assignMode = false; // Toggle for selectable state

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
  }

  // Init list and order it.
  void initList(final List<ISuspensionBean> objectsFromPage) {
    if (widget.objects.isNotEmpty) {
      if (widget.roleCheck) {
        therapists.addAll(widget.objects as List<Therapist>);
      } else {
        patients.addAll(widget.objects as List<Patient>);
      }

      SuspensionUtil.sortListBySuspensionTag(widget.objects);
      SuspensionUtil.setShowSuspensionStatus(widget.objects);
    }
    if (mounted) {
      setState(() {});
    }
  }

  // show the emojipicker within a dialog so on screen
  // also takes an patient to send an edit to submit emoji selection to db
  Future<void> showEmojiPicker(
      final TextEditingController controller, final Patient patient) {
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
                height: MediaQuery.of(context).size.height *
                    0.4, // dynamically determines height
                checkPlatformCompatibility: true,
                emojiViewConfig: const EmojiViewConfig(
                  backgroundColor: Colors.white,
                ),
                // handles the category view, top bar of the emoji picker
                categoryViewConfig: CategoryViewConfig(
                  iconColorSelected: Theme.of(context).highlightColor,
                  indicatorColor: Colors.white,
                  backgroundColor: Theme.of(context).colorScheme.primary,
                  iconColor: Colors.black,
                  dividerColor: Colors.black,
                ),
                // handles config of search bar, bottom bar of the emoji picker
                bottomActionBarConfig: BottomActionBarConfig(
                    backgroundColor: Theme.of(context).colorScheme.primary,
                    buttonColor: Theme.of(context).highlightColor,
                    buttonIconColor: Theme.of(context).primaryColor),
              ),
            ),
          );
        });
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: widget.patientMoveBar
          ? AppBar(
              backgroundColor: Theme.of(context).colorScheme.primary,
              title: Row(
                children: [
                  Expanded(
                    child: Text(widget.title!), // Title text
                  ),
                  TextButton(
                    key: const Key('move_patient_toggle'),
                    onPressed: () {
                      toggleAssignMode(); // Toggle assign mode
                    },
                    style: TextButton.styleFrom(
                      backgroundColor: assignMode ? Colors.green : Colors.red,
                      foregroundColor: Colors.black,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                    child: Text(
                        'Reassign Patients: ${assignMode == false ? 'off' : 'on'}'),
                  ),
                  const Spacer(flex: 1)
                ],
              ),
              actions: [
                Visibility(
                  visible: assignMode && selectedPatients.isNotEmpty,
                  // Show only when assignMode is true
                  maintainSize: true,
                  // Reserve space even when invisible
                  maintainAnimation: true,
                  maintainState: true,
                  child: TextButton(
                    key: const Key('toggled_reassignment_button'),
                    onPressed: () {
                      mounted
                          ? Navigator.pushReplacement(
                              context,
                              MaterialPageRoute(
                                  builder: (final context) => TherapistListPage(
                                        therapistThatWillHavePatientsMoved:
                                            therapistOld!,
                                        therapistAssignmentTitle:
                                            'Reassign ${selectedPatients.length} patient${selectedPatients.length == 1 ? '' : 's'} '
                                            'from ${therapistOld!.fName} to ...',
                                        lstPatientsReassignment:
                                            selectedPatients,
                                      )))
                          : null;
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
              ],
            )
          : null,
      body: Stack(
        children: [
          AzListView(
            key: Key('${name}_list_widget'),
            data: widget.objects,
            itemCount: widget.objects.length,
            itemBuilder: (final context, final index) {
              final identity = widget.objects[index];

              final tag = identity.getSuspensionTag();
              Color? cardColor;

              final fullName = widget.roleCheck
                  ? "${therapists[index].fName} ${therapists[index].lName}"
                  : "${patients[index].fName} ${patients[index].lName}";

              if (index == 0 ||
                  widget.objects[index].getSuspensionTag() != tag) {
                cardColor = Colors.grey[300];
              } else {
                cardColor = index % 2 == 0 ? Colors.grey[300] : Colors.blueGrey;
              }

              // assign the the emojis to the patient
              final TextEditingController _emojiController =
              TextEditingController();
              if (widget.roleCheck == false) {
                if (patients[index].emoji! == "") {
                  // if it is null, assign warning as default emoji
                  _emojiController.text = "⚠️";
                } else {
                  _emojiController.text = patients[index].emoji!;
                }
              }

              return Card(
                margin: const EdgeInsets.only(
                    left: 5, right: 5, top: 10, bottom: 10),
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
                              "${patients[index].fName.toLowerCase()}${patients[index].lName.toLowerCase()}-emoji-$index"),
                          onPressed: () => showEmojiPicker(
                              _emojiController, patients[index]),
                          child: ValueListenableBuilder(
                            // will reload display when value changes which is text controller
                              valueListenable: _emojiController,
                              builder: (context, text, child) {
                                return RichText(
                                  text: TextSpan(
                                    // dynamically size the button and the emoji based on screen size
                                    style: TextStyle(
                                        fontSize: MediaQuery.of(context)
                                            .textScaler
                                            .scale(40)),
                                    text: _emojiController.text,
                                  ),
                                );
                              }),
                        ),
                      FittedBox(
                          child: Text(fullName)), // dispaly the patient name
                    ],
                  ),
                  onTap: () {
                    if (widget.cardClick != null && !assignMode) {
                      // If not in assign mode, call the cardClick function
                      widget.cardClick!(identity);
                    } else if (assignMode) {
                      // If in assign mode, toggle the selection state
                      setState(() {
                        if (selectedPatients.contains(patients[index])) {
                          selectedPatients.remove(
                              patients[index]); // Remove if already selected
                        } else {
                          selectedPatients
                              .add(patients[index]); // Add if not selected
                        }
                      });
                    } else {
                      debugPrint('onclick not implemented and not assign mode');
                    }
                  },
                  trailing: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      // Show checkbox when in assign mode
                      if (assignMode == true)
                        Checkbox(
                          value: selectedPatients.contains(
                              patients[index]), // Check if patient is selected
                          onChanged: (final bool? value) {
                            setState(() {
                              if (value == true) {
                                selectedPatients.add(
                                    patients[index]); // Add to selected list
                              } else {
                                selectedPatients.remove(patients[
                                    index]); // Remove from selected list
                              }
                            });
                          },
                        ),
                      // Edit button
                      if (widget.editClick != null && !assignMode)
                        Tooltip(
                          message: 'Edit $name',
                          child: IconButton(
                            icon: const Icon(Icons.edit),
                            onPressed: widget.editClick != null
                                ? () => widget.editClick!(identity)
                                : () => debugPrint("edit not implemented"),
                          ),
                        ),
                    ],
                  ),
                ),
              );
            },
            susItemBuilder: (final context, final index) {
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
        ],
      ),
    );
  }
}
