// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

const API_BASE_URL = __DEV__
  ? "http://192.168.3.214:7071/api"
  : "https://<YOUR_FUNCTION_APP>.azurewebsites.net/api";

export interface ValidationResult {
  isWithinRadius: boolean;
  distanceMeters: number;
  radiusMeters: number;
  registeredAddress: string;
  deviceCoordinates: { latitude: number; longitude: number };
  addressCoordinates: { latitude: number; longitude: number };
}

export interface ApiError {
  error: string;
}

export async function validateLocation(
  userId: string,
  latitude: number,
  longitude: number
): Promise<ValidationResult> {
  const response = await fetch(`${API_BASE_URL}/validate-location`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId, latitude, longitude }),
  });

  if (!response.ok) {
    const errorBody: ApiError = await response.json();
    throw new Error(errorBody.error || `HTTP ${response.status}`);
  }

  return response.json();
}
