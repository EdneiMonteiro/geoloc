// Disclaimer
// Notice: Any sample scripts, code, or commands comes with the following notification.
//
// This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production
// environment. THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute
// the object code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to
// market Your software product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your
// software product in which the Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our
// suppliers from and against any claims or lawsuits, including attorneys' fees, that arise or result from the use or
// distribution of the Sample Code.
//
// Please note: None of the conditions outlined in the disclaimer above will supersede the terms and conditions
// contained within the Customers Support Services Description.

import React, { useState } from "react";
import {
  View,
  TouchableOpacity,
  Image,
  Text,
  StyleSheet,
  Alert,
  ActivityIndicator,
} from "react-native";
import * as ImagePicker from "expo-image-picker";
import * as Location from "expo-location";

interface CaptureResult {
  photoUri: string;
  latitude: number;
  longitude: number;
}

interface CameraViewProps {
  onCapture: (result: CaptureResult) => void;
}

export default function CameraView({ onCapture }: CameraViewProps) {
  const [photoUri, setPhotoUri] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleTakePhoto = async () => {
    try {
      setLoading(true);

      // Request camera permission
      const cameraPermission =
        await ImagePicker.requestCameraPermissionsAsync();
      if (!cameraPermission.granted) {
        Alert.alert(
          "Permissão negada",
          "É necessário permitir o acesso à câmera."
        );
        return;
      }

      // Request location permission
      const locationPermission =
        await Location.requestForegroundPermissionsAsync();
      if (locationPermission.status !== "granted") {
        Alert.alert(
          "Permissão negada",
          "É necessário permitir o acesso à localização."
        );
        return;
      }

      // Take photo
      const result = await ImagePicker.launchCameraAsync({
        mediaTypes: ["images"],
        quality: 0.5,
      });

      if (result.canceled) return;

      const uri = result.assets[0].uri;
      setPhotoUri(uri);

      // Get current GPS coordinates
      const location = await Location.getCurrentPositionAsync({
        accuracy: Location.Accuracy.High,
      });

      onCapture({
        photoUri: uri,
        latitude: location.coords.latitude,
        longitude: location.coords.longitude,
      });
    } catch (error: any) {
      Alert.alert("Erro", error.message || "Erro ao capturar foto.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <TouchableOpacity
        style={styles.button}
        onPress={handleTakePhoto}
        disabled={loading}
      >
        {loading ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={styles.buttonText}>📷 Tirar Foto</Text>
        )}
      </TouchableOpacity>

      {photoUri && (
        <Image source={{ uri: photoUri }} style={styles.preview} />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: "center",
    marginVertical: 16,
  },
  button: {
    backgroundColor: "#0078D4",
    paddingHorizontal: 32,
    paddingVertical: 14,
    borderRadius: 10,
    minWidth: 200,
    alignItems: "center",
  },
  buttonText: {
    color: "#fff",
    fontSize: 18,
    fontWeight: "600",
  },
  preview: {
    width: 280,
    height: 210,
    borderRadius: 12,
    marginTop: 16,
  },
});
