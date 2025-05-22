import 'package:azlistview/azlistview.dart';
import 'package:flutter/material.dart';
import 'package:frontend/models/patient.dart';
import 'package:intl/intl.dart';

class PatientArchiveWidget extends StatelessWidget {
  final Patient patient;
  final void Function(ISuspensionBean) onRestoreClick;
  final void Function(ISuspensionBean) onDeleteClick;
  final GlobalKey? restoreButtonKey;
  final GlobalKey? deleteButtonKey;

  const PatientArchiveWidget({
    super.key,
    required this.patient,
    required this.onRestoreClick,
    required this.onDeleteClick,
    this.restoreButtonKey,
    this.deleteButtonKey,
  });

  @override
  Widget build(final BuildContext context) {
    final String archivalDateText = patient.archivalDate != null
        ? DateFormat('MMM d, yyyy').format(patient.archivalDate!)
        : 'Unknown';

    return Card(
      elevation: 4,
      margin: const EdgeInsets.symmetric(vertical: 6, horizontal: 8),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${patient.fName ?? 'Unknown'} ${patient.lName ?? 'Unknown'}',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 18,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Condition: ${patient.condition ?? 'Not specified'}',
                    style: TextStyle(
                      fontSize: 14,
                      color: Colors.grey[600],
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Archived: $archivalDateText',
                    style: TextStyle(
                      fontSize: 14,
                      color: Colors.grey[600],
                    ),
                  ),
                ],
              ),
            ),
            Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Tooltip(
                  message: 'Restore Patient',
                  child: IconButton(
                    key: restoreButtonKey ?? Key('restore_${patient.id}'),
                    icon: const Icon(Icons.restore, color: Colors.green),
                    onPressed: () => onRestoreClick(patient),
                  ),
                ),
                Tooltip(
                  message: 'Delete Permanently',
                  child: IconButton(
                    key: deleteButtonKey ?? Key('delete_${patient.id}'),
                    icon: const Icon(Icons.delete_forever, color: Colors.red),
                    onPressed: () => onDeleteClick(patient),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}