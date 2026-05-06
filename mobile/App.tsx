// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

import React, { useState } from "react";
import { StatusBar } from "expo-status-bar";
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  KeyboardAvoidingView,
  Platform,
} from "react-native";
import CameraView from "./components/CameraView";
import ResultCard from "./components/ResultCard";
import {
  validateLocation,
  type ValidationResult,
} from "./services/apiService";

export default function App() {
  const [userId, setUserId] = useState("user001");
  const [coords, setCoords] = useState<{
    latitude: number;
    longitude: number;
  } | null>(null);
  const [photoUri, setPhotoUri] = useState<string | null>(null);
  const [result, setResult] = useState<ValidationResult | null>(null);
  const [loading, setLoading] = useState(false);

  const handleCapture = (capture: {
    photoUri: string;
    latitude: number;
    longitude: number;
  }) => {
    setPhotoUri(capture.photoUri);
    setCoords({ latitude: capture.latitude, longitude: capture.longitude });
    setResult(null); // clear previous result
  };

  const handleValidate = async () => {
    if (!userId.trim()) {
      Alert.alert("Erro", "Informe o ID do usuário.");
      return;
    }
    if (!coords) {
      Alert.alert("Erro", "Tire uma foto primeiro para capturar as coordenadas.");
      return;
    }

    try {
      setLoading(true);
      const validationResult = await validateLocation(
        userId.trim(),
        coords.latitude,
        coords.longitude
      );
      setResult(validationResult);
    } catch (error: any) {
      Alert.alert("Erro na validação", error.message || "Erro desconhecido.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === "ios" ? "padding" : "height"}
    >
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <Text style={styles.title}>GeoLoc</Text>
        <Text style={styles.subtitle}>
          Validação de localização com Azure Maps
        </Text>

        {/* User ID input */}
        <View style={styles.inputGroup}>
          <Text style={styles.label}>ID do Usuário</Text>
          <TextInput
            style={styles.input}
            value={userId}
            onChangeText={setUserId}
            placeholder="Ex: user001"
            autoCapitalize="none"
            autoCorrect={false}
          />
        </View>

        {/* Camera */}
        <CameraView onCapture={handleCapture} />

        {/* Captured coordinates */}
        {coords && (
          <View style={styles.coordsBox}>
            <Text style={styles.coordsLabel}>📍 Coordenadas capturadas:</Text>
            <Text style={styles.coordsValue}>
              {coords.latitude.toFixed(6)}, {coords.longitude.toFixed(6)}
            </Text>
          </View>
        )}

        {/* Validate button */}
        {coords && (
          <TouchableOpacity
            style={[styles.validateButton, loading && styles.buttonDisabled]}
            onPress={handleValidate}
            disabled={loading}
          >
            {loading ? (
              <ActivityIndicator color="#fff" />
            ) : (
              <Text style={styles.validateButtonText}>
                🔍 Validar Localização
              </Text>
            )}
          </TouchableOpacity>
        )}

        {/* Result */}
        {result && <ResultCard result={result} />}
      </ScrollView>
      <StatusBar style="auto" />
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F5F7FA",
  },
  scrollContent: {
    padding: 24,
    paddingTop: 60,
    paddingBottom: 40,
  },
  title: {
    fontSize: 32,
    fontWeight: "800",
    color: "#0078D4",
    textAlign: "center",
  },
  subtitle: {
    fontSize: 14,
    color: "#666",
    textAlign: "center",
    marginBottom: 24,
  },
  inputGroup: {
    marginBottom: 8,
  },
  label: {
    fontSize: 14,
    fontWeight: "600",
    color: "#333",
    marginBottom: 6,
  },
  input: {
    backgroundColor: "#fff",
    borderWidth: 1,
    borderColor: "#ddd",
    borderRadius: 10,
    paddingHorizontal: 16,
    paddingVertical: 12,
    fontSize: 16,
  },
  coordsBox: {
    backgroundColor: "#EEF2FF",
    borderRadius: 10,
    padding: 12,
    alignItems: "center",
    marginVertical: 8,
  },
  coordsLabel: {
    fontSize: 13,
    color: "#555",
  },
  coordsValue: {
    fontSize: 15,
    fontWeight: "600",
    color: "#333",
    fontFamily: "monospace",
    marginTop: 4,
  },
  validateButton: {
    backgroundColor: "#107C10",
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: "center",
    marginTop: 12,
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  validateButtonText: {
    color: "#fff",
    fontSize: 18,
    fontWeight: "600",
  },
});
