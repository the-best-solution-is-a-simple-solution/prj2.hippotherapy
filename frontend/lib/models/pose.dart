// Database Key values
import 'package:frontend/models/evaluation.dart';

const String dbKeyElbowExtension = "elbow_extension";
const String dbKeyHead = "head";
const String dbKeyHeadAnt = "head_ant";
const String dbKeyHipFlex = "hip_flex";
const String dbKeyKneeFlex = "knee_flex";
const String dbKeyLumber = "lumbar";
const String dbKeyPelvic = "pelvic";
const String dbKeyPelvicTilt = "pelvic_tilt";
const String dbKeyThoracic = "thoracic";
const String dbKeyTrunk = "trunk";
const String dbKeyTrunkInclination = "trunk_inclination";

//img path default
const String postureAssetsPath = "../assets/images/posture/";

/// An enum which has the groupings for each image, displayName
/// is a user friendly name with spaces
enum PoseGroup {
  HEAD("${postureAssetsPath}head_3.png", "Head", "Head"),
  ELBOW_EXTENSION(
      "${postureAssetsPath}elbow_extension_2.png", "Elbow Extension", "Arm"),
  THORACIC("${postureAssetsPath}thoracic_3.png", "Thoracic", "Thorax"),
  HEAD_ANT("${postureAssetsPath}head_ant_3.png", "Head Anterior", "Head"),
  HIP_FLEX("${postureAssetsPath}hip_flex_2.png", "Hip Flex", "Hip"),
  KNEE_FLEX("${postureAssetsPath}hip_flex_2.png", "Knee Flex", "Leg"),
  LUMBAR("${postureAssetsPath}lumber_3.png", "Lumbar", "Thorax"),
  PELVIC("${postureAssetsPath}pelvic_3.png", "Pelvic", "Hip"),
  PELVIC_TILT("${postureAssetsPath}pelvic_tilt_3.png", "Pelvic Tilt", "Hip"),
  TRUNK("${postureAssetsPath}trunk_3.png", "Trunk", "Hip"),
  TRUNK_INCLINATION("${postureAssetsPath}trunk_inclination_3.png",
      "Trunk Inclination", "Hip");

  const PoseGroup(this.defaultNeutralPositionImage, this.displayName, this.tab);

  final String defaultNeutralPositionImage;
  final String displayName;
  final String tab;
}

enum PoseType {
  // Elbow Extension
  ELBOW_EXTENSION_NEG_ONE(PoseGroup.ELBOW_EXTENSION, -1, dbKeyElbowExtension,
      "${postureAssetsPath}elbow_extension_1.png"),
  ELBOW_EXTENSION_NEUTRAL(PoseGroup.ELBOW_EXTENSION, 0, dbKeyElbowExtension,
      "${postureAssetsPath}elbow_extension_2.png"),
  ELBOW_EXTENSION_POS_ONE(PoseGroup.ELBOW_EXTENSION, 1, dbKeyElbowExtension,
      "${postureAssetsPath}elbow_extension_3.png"),

  // Head
  HEAD_NEG_TWO(PoseGroup.HEAD, -2, dbKeyHead, "${postureAssetsPath}head_1.png"),
  HEAD_NEG_ONE(PoseGroup.HEAD, -1, dbKeyHead, "${postureAssetsPath}head_2.png"),
  HEAD_NEUTRAL(PoseGroup.HEAD, 0, dbKeyHead, "${postureAssetsPath}head_3.png"),
  HEAD_POS_ONE(PoseGroup.HEAD, 1, dbKeyHead, "${postureAssetsPath}head_4.png"),
  HEAD_POS_TWO(PoseGroup.HEAD, 2, dbKeyHead, "${postureAssetsPath}head_5.png"),

  // Head Ant
  HEAD_ANT_NEG_TWO(PoseGroup.HEAD_ANT, -2, dbKeyHeadAnt,
      "${postureAssetsPath}head_ant_1.png"),
  HEAD_ANT_NEG_ONE(PoseGroup.HEAD_ANT, -1, dbKeyHeadAnt,
      "${postureAssetsPath}head_ant_2.png"),
  HEAD_ANT_NEUTRAL(PoseGroup.HEAD_ANT, 0, dbKeyHeadAnt,
      "${postureAssetsPath}head_ant_3.png"),
  HEAD_ANT_POS_ONE(PoseGroup.HEAD_ANT, 1, dbKeyHeadAnt,
      "${postureAssetsPath}head_ant_4.png"),
  HEAD_ANT_POS_TWO(PoseGroup.HEAD_ANT, 2, dbKeyHeadAnt,
      "${postureAssetsPath}head_ant_5.png"),

  // Hip Flex
  HIP_FLEX_NEG_ONE(PoseGroup.HIP_FLEX, -1, dbKeyHipFlex,
      "${postureAssetsPath}hip_flex_1.png"),
  HIP_FLEX_NEUTRAL(PoseGroup.HIP_FLEX, 0, dbKeyHipFlex,
      "${postureAssetsPath}hip_flex_2.png"),
  HIP_FLEX_POS_ONE(PoseGroup.HIP_FLEX, 1, dbKeyHipFlex,
      "${postureAssetsPath}hip_flex_3.png"),

  // Knee Flex
  KNEE_FLEX_NEG_TWO(PoseGroup.KNEE_FLEX, -2, dbKeyKneeFlex,
      "${postureAssetsPath}knee_flex_1.png"),
  KNEE_FLEX_NEG_ONE(PoseGroup.KNEE_FLEX, -1, dbKeyKneeFlex,
      "${postureAssetsPath}knee_flex_2.png"),
  KNEE_FLEX_NEUTRAL(PoseGroup.KNEE_FLEX, 0, dbKeyKneeFlex,
      "${postureAssetsPath}knee_flex_3.png"),
  KNEE_FLEX_POS_ONE(PoseGroup.KNEE_FLEX, 1, dbKeyKneeFlex,
      "${postureAssetsPath}knee_flex_4.png"),
  KNEE_FLEX_POS_TWO(PoseGroup.KNEE_FLEX, 2, dbKeyKneeFlex,
      "${postureAssetsPath}knee_flex_5.png"),

  // Lumber
  LUMBER_NEG_TWO(
      PoseGroup.LUMBAR, -2, dbKeyLumber, "${postureAssetsPath}lumber_1.png"),
  LUMBER_NEG_ONE(
      PoseGroup.LUMBAR, -1, dbKeyLumber, "${postureAssetsPath}lumber_2.png"),
  LUMBER_NEUTRAL(
      PoseGroup.LUMBAR, 0, dbKeyLumber, "${postureAssetsPath}lumber_3.png"),
  LUMBER_POS_ONE(
      PoseGroup.LUMBAR, 1, dbKeyLumber, "${postureAssetsPath}lumber_4.png"),
  LUMBER_POS_TWO(
      PoseGroup.LUMBAR, 2, dbKeyLumber, "${postureAssetsPath}lumber_5.png"),

  // Pelvic
  PELVIC_NEG_TWO(
      PoseGroup.PELVIC, -2, dbKeyPelvic, "${postureAssetsPath}pelvic_1.png"),
  PELVIC_NEG_ONE(
      PoseGroup.PELVIC, -1, dbKeyPelvic, "${postureAssetsPath}pelvic_2.png"),
  PELVIC_NEUTRAL(
      PoseGroup.PELVIC, 0, dbKeyPelvic, "${postureAssetsPath}pelvic_3.png"),
  PELVIC_POS_ONE(
      PoseGroup.PELVIC, 1, dbKeyPelvic, "${postureAssetsPath}pelvic_4.png"),
  PELVIC_POS_TWO(
      PoseGroup.PELVIC, 2, dbKeyPelvic, "${postureAssetsPath}pelvic_5.png"),

  // Pelvic Tilt
  PELVIC_TILT_NEG_TWO(PoseGroup.PELVIC_TILT, -2, dbKeyPelvicTilt,
      "${postureAssetsPath}pelvic_tilt_1.png"),
  PELVIC_TILT_NEG_ONE(PoseGroup.PELVIC_TILT, -1, dbKeyPelvicTilt,
      "${postureAssetsPath}pelvic_tilt_2.png"),
  PELVIC_TILT_NEUTRAL(PoseGroup.PELVIC_TILT, 0, dbKeyPelvicTilt,
      "${postureAssetsPath}pelvic_tilt_3.png"),
  PELVIC_TILT_POS_ONE(PoseGroup.PELVIC_TILT, 1, dbKeyPelvicTilt,
      "${postureAssetsPath}pelvic_tilt_4.png"),
  PELVIC_TILT_POS_TWO(PoseGroup.PELVIC_TILT, 2, dbKeyPelvicTilt,
      "${postureAssetsPath}pelvic_tilt_5.png"),

  // Thoracic
  THORACIC_NEG_TWO(PoseGroup.THORACIC, -2, dbKeyThoracic,
      "${postureAssetsPath}thoracic_1.png"),
  THORACIC_NEG_ONE(PoseGroup.THORACIC, -1, dbKeyThoracic,
      "${postureAssetsPath}thoracic_2.png"),
  THORACIC_NEUTRAL(PoseGroup.THORACIC, 0, dbKeyThoracic,
      "${postureAssetsPath}thoracic_3.png"),
  THORACIC_POS_ONE(PoseGroup.THORACIC, 1, dbKeyThoracic,
      "${postureAssetsPath}thoracic_4.png"),
  THORACIC_POS_TWO(PoseGroup.THORACIC, 2, dbKeyThoracic,
      "${postureAssetsPath}thoracic_5.png"),

  // Trunk
  TRUNK_NEG_TWO(
      PoseGroup.TRUNK, -2, dbKeyTrunk, "${postureAssetsPath}trunk_1.png"),
  TRUNK_NEG_ONE(
      PoseGroup.TRUNK, -1, dbKeyTrunk, "${postureAssetsPath}trunk_2.png"),
  TRUNK_NEUTRAL(
      PoseGroup.TRUNK, 0, dbKeyTrunk, "${postureAssetsPath}trunk_3.png"),
  TRUNK_POS_ONE(
      PoseGroup.TRUNK, 1, dbKeyTrunk, "${postureAssetsPath}trunk_4.png"),
  TRUNK_POS_TWO(
      PoseGroup.TRUNK, 2, dbKeyTrunk, "${postureAssetsPath}trunk_5.png"),

  // Trunk Inclination
  TRUNK_INCLINATION_NEG_TWO(PoseGroup.TRUNK_INCLINATION, -2,
      dbKeyTrunkInclination, "${postureAssetsPath}trunk_inclination_1.png"),
  TRUNK_INCLINATION_NEG_ONE(PoseGroup.TRUNK_INCLINATION, -1,
      dbKeyTrunkInclination, "${postureAssetsPath}trunk_inclination_2.png"),
  TRUNK_INCLINATION_NEUTRAL(PoseGroup.TRUNK_INCLINATION, 0,
      dbKeyTrunkInclination, "${postureAssetsPath}trunk_inclination_3.png"),
  TRUNK_INCLINATION_POS_ONE(PoseGroup.TRUNK_INCLINATION, 1,
      dbKeyTrunkInclination, "${postureAssetsPath}trunk_inclination_4.png"),
  TRUNK_INCLINATION_POS_TWO(PoseGroup.TRUNK_INCLINATION, 2,
      dbKeyTrunkInclination, "${postureAssetsPath}trunk_inclination_5.png");

  const PoseType(this.category, this.value, this.dbCategoryKey, this.imgPath);

  final PoseGroup category;
  final num value;
  final String dbCategoryKey;
  final String imgPath;
}

///For converting PatientEvaluation model to list of PoseType
class ConvertEvaluationToPoseType {
  /// Converts the provided evaluation to a list of pose-types
  static List<PoseType> convertToPoseTypes(final PatientEvaluation eval) {
    // create the appropriate poseType based on the eval property
    final List<PoseType> poses = [];

    // List of pose groups
    // pass in the group and value get back pose type
    final Map<String, int> poseGroups = {
      dbKeyHipFlex: eval.hipFlex,
      dbKeyLumber: eval.lumbar,
      dbKeyHeadAnt: eval.headAnt,
      dbKeyHead: eval.headLat,
      dbKeyKneeFlex: eval.kneeFlex,
      dbKeyPelvic: eval.pelvic,
      dbKeyPelvicTilt: eval.pelvicTilt,
      dbKeyThoracic: eval.thoracic,
      dbKeyTrunk: eval.trunk,
      dbKeyTrunkInclination: eval.trunkInclincation,
      dbKeyElbowExtension: eval.elbowExtension
    };

    // go through the converting each one
    for (final keyValue in poseGroups.entries) {
      poses.add(convertToPoseType(keyValue.key, keyValue.value));
    }

    return poses;
  }

  /// Convert to poseType using dbCategoryKey and a value
  static PoseType convertToPoseType(
      final String dbCategoryKey, final int value) {
    // loop through poses check if one matches key and value
    for (final pose in PoseType.values) {
      if (pose.dbCategoryKey == dbCategoryKey && pose.value == value) {
        return pose;
      }
    }
    throw Exception(
        'PoseType conversion error group key: $dbCategoryKey and value: $value not found');
  }
}
