{{- define "mcma.labels" }}
serviceGroup: "mcma"
serviceName: {{ .serviceName | quote }}
serviceFunction: {{ .serviceFunction | quote }}
{{- end }}

{{- define "mcma.deployment" }}
apiVersion: apps/v1
kind: Deployment
metadata:
  name: "{{ .serviceName }}-{{ .serviceFunction }}"
  labels:
{{- include "mcma.labels" . | indent 4 }}
spec:
  replicas: {{ .functionConfig.numReplicas }}
  selector:
    matchLabels:
{{- include "mcma.labels" . | indent 6 }}
  template:
    metadata:
      labels:
{{- include "mcma.labels" . | indent 8 }}
    spec:
      containers:
        - name: {{ .serviceName | quote }}
          image: "{{ .functionConfig.dockerImageId }}:{{ .version }}"
          ports:
            - containerPort: 80
          envFrom:
            - configMapRef:
                name: "{{ .serviceName }}-env-vars"
{{- end }}