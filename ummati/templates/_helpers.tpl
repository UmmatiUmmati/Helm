{{/* vim: set filetype=mustache: */}}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "ummati.fullname" -}}
{{- $name := .Chart.Name -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "ummati.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create default labels to be added to every resource.
See https://kubernetes.io/docs/concepts/overview/working-with-objects/common-labels/
*/}}
{{- define "ummati.defaultLabels" -}}
app.kubernetes.io/instance: {{ .Release.Name | quote }}
app.kubernetes.io/version: {{ .Chart.AppVersion }}
app.kubernetes.io/part-of: {{ .Chart.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service | quote }}
helm.sh/chart: {{ include "ummati.chart" . | quote }}
ummati/environment: {{ .Values.environment | quote }}
{{- end -}}

{{/*
Create default annotations to be added to every resource.
*/}}
{{- define "ummati.defaultAnnotations" -}}
ummati/chartName: {{ .Chart.Name | quote }}
ummati/chartVersion: {{ .Chart.Version | quote }}
ummati/releaseTime: {{ date "2006-01-02 15:04:05 -0700" .Release.Time | quote }}
ummati/releaseRevision: {{ .Release.Revision | quote }}
ummati/releaseIsUpgrade: {{ .Release.IsUpgrade | quote }}
ummati/releaseIsInstall: {{ .Release.IsInstall | quote }}
{{- end -}}