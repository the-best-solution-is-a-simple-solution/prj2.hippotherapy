import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/models/therapist.dart';
import 'package:frontend/widgets/navigation_widgets/drawer_widget.dart';
import 'package:provider/provider.dart';

// Page to display a Therapist's information upon login.
class ProfilePage extends StatefulWidget {
  static const String RouteName = '/profile';
  const ProfilePage({super.key});

  @override
  _ProfilePageState createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  Therapist? therapist;
  bool _isLoading = true;
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    _fetchTherapistData();
  }

  // Method to get Therapist's data from db
  Future<void> _fetchTherapistData() async {
    final authController = Provider.of<AuthController>(context, listen: false);
    try {
      final String? therapistId = authController.therapistId;
      if (therapistId == null) {
        throw Exception('Therapist ID not found');
      }
      therapist = await authController.getTherapistInfo(therapistId);
    } catch (e) {
      _hasError = true;
      debugPrint("Error fetching therapist data: $e");
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(final BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Profile'),
        backgroundColor: Theme.of(context).colorScheme.primary,
        leading: Builder(
          builder: (final BuildContext context) {
            return IconButton(
              icon: const Icon(Icons.menu),
              onPressed: () {
                Scaffold.of(context).openDrawer();
              },
              tooltip: 'Menu',
            );
          },
        ),
      ),
      drawer: const HippoAppDrawer(),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _hasError
              ? const Center(
                  child: Text('Error loading profile. Please try again.'),
                )
              : therapist != null
                  ? _buildProfileContent()
                  : const Center(child: Text('No profile data available.')),
    );
  }

  Widget _buildProfileContent() {
    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Welcome, ${therapist!.fName} ${therapist!.lName}!',
              style:
                  const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
          _listAllInformation(),
        ],
      ),
    );
  }

  // Widget to create rows for Therapist information
  Widget _listAllInformation() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildInfoRow('ID', therapist!.therapistID,
            rowKey: const ValueKey('idRow')),
        _buildInfoRow('Email', therapist!.email,
            rowKey: const ValueKey('emailRow')),
        _buildInfoRow('Country', therapist!.country,
            rowKey: const ValueKey('countryRow')),
        _buildInfoRow('City', therapist!.city,
            rowKey: const ValueKey('cityRow')),
        _buildInfoRow('Street', therapist!.street,
            rowKey: const ValueKey('streetRow')),
        _buildInfoRow('Postal Code', therapist!.postalCode,
            rowKey: const ValueKey('postalCodeRow')),
        _buildInfoRow('Phone', therapist!.phone,
            rowKey: const ValueKey('phoneRow')),
        _buildInfoRow('Profession', therapist!.profession,
            rowKey: const ValueKey('professionRow')),
        _buildInfoRow('Major', therapist!.major,
            rowKey: const ValueKey('majorRow')),
        _buildInfoRow(
          'Years of Experience in Hippotherapy',
          therapist!.yearsExperienceInHippotherapy?.toString() ?? 'N/A',
          rowKey: const ValueKey('experienceRow'),
        ),
      ],
    );
  }

  Widget _buildInfoRow(final String label, final String? value,
      {final Key? rowKey}) {
    if (value == null || value.isEmpty) {
      return Container();
    }

    // print('Rendering row with key: $rowKey, label: $label, value: $value');
    return Padding(
      padding: const EdgeInsets.only(bottom: 8.0),
      child: Row(
        key: rowKey,
        children: [
          Text('$label: ', style: const TextStyle(fontWeight: FontWeight.bold)),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}
