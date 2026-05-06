// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

import React from "react";
import { View, Text, StyleSheet } from "react-native";
import type { ValidationResult } from "../services/apiService";

interface ResultCardProps {
  result: ValidationResult;
}

export default function ResultCard({ result }: ResultCardProps) {
  const isWithin = result.isWithinRadius;

  return (
    <View style={[styles.card, isWithin ? styles.success : styles.failure]}>
      <Text style={styles.statusIcon}>{isWithin ? "✅" : "❌"}</Text>
      <Text style={styles.statusText}>
        {isWithin
          ? "Dentro do raio de 50m!"
          : "Fora do raio de 50m"}
      </Text>

      <View style={styles.divider} />

      <View style={styles.row}>
        <Text style={styles.label}>Distância:</Text>
        <Text style={styles.value}>
          {result.distanceMeters.toFixed(1)} metros
        </Text>
      </View>

      <View style={styles.row}>
        <Text style={styles.label}>Raio permitido:</Text>
        <Text style={styles.value}>{result.radiusMeters} metros</Text>
      </View>

      <View style={styles.row}>
        <Text style={styles.label}>Endereço cadastrado:</Text>
        <Text style={[styles.value, styles.address]}>
          {result.registeredAddress}
        </Text>
      </View>

      <View style={styles.divider} />

      <Text style={styles.coordsTitle}>Coordenadas do dispositivo:</Text>
      <Text style={styles.coords}>
        {result.deviceCoordinates.latitude.toFixed(6)},{" "}
        {result.deviceCoordinates.longitude.toFixed(6)}
      </Text>

      <Text style={styles.coordsTitle}>Coordenadas do endereço:</Text>
      <Text style={styles.coords}>
        {result.addressCoordinates.latitude.toFixed(6)},{" "}
        {result.addressCoordinates.longitude.toFixed(6)}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 16,
    padding: 20,
    marginVertical: 16,
    marginHorizontal: 4,
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 8,
    elevation: 4,
  },
  success: {
    backgroundColor: "#E6F4EA",
    borderColor: "#34A853",
    borderWidth: 1,
  },
  failure: {
    backgroundColor: "#FDECEA",
    borderColor: "#EA4335",
    borderWidth: 1,
  },
  statusIcon: {
    fontSize: 40,
    textAlign: "center",
  },
  statusText: {
    fontSize: 20,
    fontWeight: "700",
    textAlign: "center",
    marginTop: 8,
    color: "#333",
  },
  divider: {
    height: 1,
    backgroundColor: "#ddd",
    marginVertical: 14,
  },
  row: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginVertical: 4,
  },
  label: {
    fontSize: 14,
    color: "#666",
    fontWeight: "500",
  },
  value: {
    fontSize: 14,
    color: "#333",
    fontWeight: "600",
  },
  address: {
    flex: 1,
    textAlign: "right",
    marginLeft: 8,
  },
  coordsTitle: {
    fontSize: 12,
    color: "#888",
    marginTop: 6,
  },
  coords: {
    fontSize: 13,
    color: "#555",
    fontFamily: "monospace",
  },
});
