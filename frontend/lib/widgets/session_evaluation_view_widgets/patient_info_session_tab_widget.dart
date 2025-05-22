import 'package:flutter/material.dart';
import 'package:frontend/models/patient.dart';
import 'package:frontend/models/session.dart';
import 'package:frontend/pages/patient_session_view.dart';
import 'package:intl/intl.dart';
import 'package:sticky_grouped_list/sticky_grouped_list.dart';

///A tab in patient info to display the list of sessions
class PatientInfoSessionTab extends StatefulWidget {
  const PatientInfoSessionTab({
    super.key,
    required this.sessions,
    required this.patient,
    this.sessionItemKey,
  });

  final List<Session> sessions;
  final Patient patient;
  final GlobalKey? sessionItemKey;

  @override
  State<PatientInfoSessionTab> createState() => _PatientInfoSessionTabState();
}

class _PatientInfoSessionTabState extends State<PatientInfoSessionTab> {
  bool isAscending = true;
  bool keepLoading = true;
  late List<Session> sessions;

// Initialize state of widget
  @override
  void initState() {
    super.initState();
    sessions = widget.sessions;
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading:
            false, // Prevents the back button from showing
        actions: [
          TextButton.icon(
            key: const Key('SessionsDateSortButton'),
            icon: Icon(isAscending ? Icons.arrow_downward : Icons.arrow_upward,
                color: Colors.black),
            label: const Text('Date', style: TextStyle(color: Colors.black)),
            onPressed: () {
              setState(() {
                isAscending = !isAscending; // Toggle sort order
              });
            },
          ),
        ],
      ),
// In case where sessions is not empty, load the list view sub widget
      body: sessions.isNotEmpty
// See https://pub.dev/packages/sticky_grouped_list/example
          ? StickyGroupedListView<Session, DateTime>(
              elements: sessions,
              order: StickyGroupedListOrder.DESC,
              groupBy: (final Session session) => DateTime(
                session.dateTaken.year,
              ),
// Compare groups (years)
              groupComparator: (final DateTime date1, final DateTime date2) =>
                  isAscending ? date1.compareTo(date2) : date2.compareTo(date1),
// Sort items within groups by full date
              itemComparator: (final Session s1, final Session s2) =>
                  isAscending
                      ? s1.dateTaken.compareTo(s2.dateTaken)
                      : s2.dateTaken.compareTo(s1.dateTaken),
              groupSeparatorBuilder: _getGroupSeparator,
              itemBuilder: (final context, final session) {
                final index = sessions.indexOf(session);
                return _getItem(context, session, index);
              },
            )
          : keepLoading
              ? const Center(child: CircularProgressIndicator())
              : const Center(
                  child: Text("No sessions found.",
                      style: TextStyle(fontSize: 20, color: Colors.red))),
    );
  }

  Widget _getGroupSeparator(final Session session) {
    return SizedBox(
      height: 50,
      child: Align(
        alignment: Alignment.center,
        child: Container(
          width: 120,
          decoration: BoxDecoration(
            color: Colors.blue[300],
            border: Border.all(
              color: Colors.blue[300]!,
            ),
            borderRadius: const BorderRadius.all(Radius.circular(20.0)),
          ),
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: Text(
              '${session.dateTaken.year}',
              textAlign: TextAlign.center,
            ),
          ),
        ),
      ),
    );
  }

  Widget _getItem(
      final BuildContext ctx, final Session session, final int index) {
    return Card(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(6.0),
      ),
      elevation: 8.0,
      margin: const EdgeInsets.symmetric(horizontal: 10.0, vertical: 6.0),
      child: SizedBox(
        child: ListTile(
          key: index == 0 ? widget.sessionItemKey : null,
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 20.0, vertical: 10.0),
          leading: const Icon(Icons.assessment),
          title: Text(
              DateFormat('yyyy-MM-dd\t\t\t\t\tH:mm').format(session.dateTaken)),
          onTap: () {
            Navigator.push(
                context,
                MaterialPageRoute(
                    builder: (final context) => PatientSessionView(
                          patient: widget.patient,
                          session: session,
                          title:
                              '${widget.patient.fName} ${widget.patient.lName}, ${DateFormat('yyyy-MM-dd\t\t\t\t\tH:mm').format(session.dateTaken)}, Evaluations',
                        )
                    // EvaluationComparisonPage(
                    //       patientID: widget.patientID,
                    //       sessionID: session.sessionID!,
                    //       title: 'Pre/Post Comparison',
                    //     )
                    ));
          },
        ),
      ),
    );
  }
}
